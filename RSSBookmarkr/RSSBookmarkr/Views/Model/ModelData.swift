import Foundation

@Observable
class ModelData {
    var user: User?
    var urls: [URL] = []
    var rssList: [RSS] = []
    
    let rssBookrmarkrClient: RSSBookmarkrClient
    
    init(rssBookrmarkrClient: RSSBookmarkrClient = RSSBookmarkrClient()) {
        self.rssBookrmarkrClient = rssBookrmarkrClient
    }
    
    func initUser() async throws {
        let loginResponse = try await rssBookrmarkrClient.initLogin(request:  InitLoginRequest(sessionId: "TODO"))
        switch loginResponse {
        case .success(let profile):
            self.user = User(userId: profile.userId, sessionId: profile.sessionId, email: profile.email)
        default:
            break
        }
    }
    
    func login(username: String, password: String) async throws {
        let loginResponse = try await rssBookrmarkrClient.loginOrRegister(request:  LoginRequest(username: username, password: password))
        switch loginResponse {
        case .success(let profile):
            self.user = User(userId: profile.userId, sessionId: profile.sessionId, email: profile.email)
        default:
            break
        }
    }
    
    func logout() {
        self.user = nil
    }
    
    func addUrl(_ url: URL) {
        self.urls.append(url)
        Task {
            try? await self.getRSSList(self.urls)
        }
    }
    
    func setUrls(_ urls: [URL]) {
        self.urls = urls
    }
    
    func removeUrl(url: URL) {
        self.urls = self.urls.filter { $0 != url }
        Task {
            try? await self.getRSSList(self.urls)
        }
    }
    
    func saveUrls() async throws {
        guard let userId = user?.userId else {
            throw RSSError.unauthenticated
        }
        
        try await rssBookrmarkrClient.saveRSSUrls(request: SaveRSSUrlRequest(userId: userId, urls: self.urls))
    }
    
    func getRSSList(_ urls: [URL]) async throws {
        self.rssList = try await rssBookrmarkrClient.getRSSList(from: urls)
    }
    
    func subscribe() async throws {
        guard let userId = user?.userId,
              let email = user?.email
        else {
            throw RSSError.unauthenticated
        }
        
        try await rssBookrmarkrClient.subscribe(request: SubscribeRequest(userId: userId, email: email))
    }

    func unsubscribe() async throws {
        guard let userId = user?.userId,
              let email = user?.email
        else {
            throw RSSError.unauthenticated
        }
        
        try await rssBookrmarkrClient.unsubscribe(request: UnsubscribeRequest(email: email))
    }
}
