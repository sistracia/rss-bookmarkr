import Foundation

@Observable
class ModelData {
    var user: User?
    var urls: [URL] = []
    var rssList: [RSS] = []
    
    var serverState: ServerState = .idle
    
    enum ServerState {
        case idle
        case loading
        case error(String)
    }
    
    let rssBookrmarkrClient: RSSBookmarkrClient
    
    init(rssBookrmarkrClient: RSSBookmarkrClient = RSSBookmarkrClient()) {
        self.rssBookrmarkrClient = rssBookrmarkrClient
    }
    
    func serverCall<T>(defaultReturn: T, callback: () async throws -> T) async -> T {
        serverState = .loading
        do {
            serverState = .idle
            return try await callback()
        } catch (let error) {
            serverState = .error(error.localizedDescription)
        }
        
        return defaultReturn
    }
    
    func loginSuccess(loginResponse: LoginResponse) {
        switch loginResponse {
        case .success(let profile):
            self.user = User(userId: profile.userId, sessionId: profile.sessionId, email: profile.email)
        default:
            break
        }
    }
    
    func initUser(sessionId: String) async {
        await serverCall(defaultReturn: ()) {
            let loginResponse = try await rssBookrmarkrClient.initLogin(request:  InitLoginRequest(sessionId: sessionId))
            loginSuccess(loginResponse: loginResponse)
        }
    }
    
    func login(username: String, password: String) async {
        await serverCall(defaultReturn: ()) {
            let loginResponse = try await rssBookrmarkrClient.loginOrRegister(request:  LoginRequest(username: username, password: password))
            loginSuccess(loginResponse: loginResponse)
        }
    }
    
    func logout() {
        self.user = nil
    }
    
    func protectedResource<T>(defaultReturn: T, callback: (User) async throws -> T) async -> T {
        let result = await serverCall(defaultReturn: defaultReturn) {
            if let user = self.user {
                let result = try await callback(user)
                return result
            } else {
                self.serverState = .error(RSSError.unauthenticated.localizedDescription)
                return defaultReturn
            }
        }
        
        return result
    }
    
    func refreshRSSListAfterAction(callback: () -> Void) {
        callback()
        Task {
            await self.getRSSList(self.urls)
        }
    }
    
    func addUrl(_ url: URL) {
        refreshRSSListAfterAction {
            self.urls.append(url)
        }
    }
    
    func setUrls(_ urls: [URL]) {
        self.urls = urls
    }
    
    func removeUrl(url: URL) {
        refreshRSSListAfterAction {
            self.urls = self.urls.filter { $0 != url }
        }
    }
    
    func saveUrls() async {
        await protectedResource(defaultReturn: ()) { user in
            try await rssBookrmarkrClient.saveRSSUrls(request: SaveRSSUrlRequest(userId: user.userId, urls: self.urls))
        }
    }
    
    func getRSSList(_ urls: [URL]) async {
        self.rssList = await serverCall(defaultReturn: []) {
            try await self.rssBookrmarkrClient.getRSSList(from: urls)
        }
    }
    
    func subscribe() async {
        await protectedResource(defaultReturn: ()) { user in
            try await rssBookrmarkrClient.subscribe(request: SubscribeRequest(userId: user.userId, email: user.email))
        }
    }
    
    func unsubscribe() async {
        await protectedResource(defaultReturn: ()) { user in
            try await rssBookrmarkrClient.unsubscribe(request: UnsubscribeRequest(email: user.email))
        }
    }
}
