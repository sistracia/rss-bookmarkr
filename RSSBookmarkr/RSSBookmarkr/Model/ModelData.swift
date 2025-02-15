import Foundation

@Observable
class ModelData {
    var user: User?
    var urls: [URL] = []
    var rssList: [RSS] = []
    private let defaultRSS = Array(repeating: RSS.placeholder, count: 10)
    
    var serverState: ServerState = .idle
    
    enum ServerState: Equatable {
        case idle
        case loading
        case error(String)
    }
    
    let rssBookrmarkrClient: RSSBookmarkrClient
    
    init(rssBookrmarkrClient: RSSBookmarkrClient = RSSBookmarkrClient()) {
        self.rssBookrmarkrClient = rssBookrmarkrClient
    }
    
    func serverCall<T>(defaultReturn: T, retryDelay: Duration = .seconds(2), callback: () async throws -> T) async -> T {
        if serverState != .idle {
            try? await Task.sleep(for: retryDelay)
            return await serverCall(defaultReturn: defaultReturn, callback: callback)
        }

        serverState = .loading
        do {
            let result = try await callback()
            serverState = .idle
            return result
        } catch (let error) {
            serverState = .error(error.localizedDescription)
        }
        
        return defaultReturn
    }
    
    func refreshRSSListAfterAction(callback: () -> Void) {
        callback()
        Task {
            await self.getRSSList(self.urls)
        }
    }
    
    func loginSuccess(loginResponse: LoginResponse) {
        refreshRSSListAfterAction {
            switch loginResponse {
            case .success(let profile):
                self.user = User(userId: profile.userId, sessionId: profile.sessionId, email: profile.email)
                self.urls = profile.rssUrls
            default:
                break
            }
        }
    }
    
    func initUser(sessionId: String) async {
        let loginResponse = await serverCall(defaultReturn: nil) {
            try await self.rssBookrmarkrClient.initLogin(request:  InitLoginRequest(sessionId: sessionId))
        }
        if let loginResponse = loginResponse {
            loginSuccess(loginResponse: loginResponse)
        }
    }
    
    func login(username: String, password: String) async {
        let loginResponse = await serverCall(defaultReturn: nil) {
            try await self.rssBookrmarkrClient.loginOrRegister(request:  LoginRequest(username: username, password: password))
        }
        if let loginResponse = loginResponse {
            loginSuccess(loginResponse: loginResponse)
        }
    }
    
    func logout() {
        self.user = nil
    }
    
    func protectedResource<T>(defaultReturn: T, callback: (User) async throws -> T) async -> T {
        return await serverCall(defaultReturn: defaultReturn) {
            if let user = self.user, let result = try? await callback(user) {
                return result
            } else {
                self.serverState = .error(RSSError.unauthenticated.localizedDescription)
                return defaultReturn
            }
        }
    }
    
    func addUrl(_ url: URL) {
        if self.urls.contains(where: { urlState in
            urlState.absoluteString == url.absoluteString
        }) {
            return
        }
        refreshRSSListAfterAction {
            self.urls.append(url)
        }
    }
    
    func setUrls(_ urls: [URL]) {
        refreshRSSListAfterAction {
            self.urls = urls
        }
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
        self.rssList = self.defaultRSS
        self.rssList = await serverCall(defaultReturn: []) {
            try await self.rssBookrmarkrClient.getRSSList(from: urls)
        }
    }
    
    func subscribe(email: String) async {
        await protectedResource(defaultReturn: ()) { user in
            self.user = User(userId: user.userId, sessionId: user.sessionId, email: email)
            try await rssBookrmarkrClient.subscribe(request: SubscribeRequest(userId: user.userId, email: email))
        }
    }
    
    func unsubscribe() async {
        await protectedResource(defaultReturn: ()) { user in
            self.user = User(userId: user.userId, sessionId: user.sessionId, email: "")
            try await rssBookrmarkrClient.unsubscribe(request: UnsubscribeRequest(email: user.email))
        }
    }
}
