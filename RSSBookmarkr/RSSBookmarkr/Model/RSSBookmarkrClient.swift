import Foundation

actor RSSBookmarkrClient {
    private let baseURL = URL(
        string: "https://rssbookmarkr.sistracia.com/rpc/IRPCStore/"
    )!
    
    private let httpClient: any RSSHTTPClient
    
    private lazy var jsonDecoder: JSONDecoder = {
        let jsonDecoder = JSONDecoder()
        return jsonDecoder
    }()
    
    private lazy var jsonEncoder: JSONEncoder = {
        let jsonEncoder = JSONEncoder()
        return jsonEncoder
    }()
    
    init(httpClient: any RSSHTTPClient = URLSession.shared) {
        self.httpClient = httpClient
    }
    
    private func request(endpoint: String, body: Data? = nil) async throws -> Data {
        guard let url = URL(string: endpoint, relativeTo: baseURL) else {
            throw RSSError.invalidURL
        }
        
        var request = URLRequest(url: url)
        request.httpMethod = "POST"
        request.httpBody = body
        request.setValue("application/json", forHTTPHeaderField: "Content-Type")
        
        let data = try await httpClient.httpData(for: request)
        return data
    }
    
    func initLogin(request req: InitLoginRequest) async throws -> LoginResponse {
        let body = try jsonEncoder.encode([req])
        let data = try await request(endpoint: "initLogin", body: body)
        let response = try jsonDecoder.decode(LoginResponse.self, from: data)
        return response
    }
    
    func loginOrRegister(request req: LoginRequest) async throws -> LoginResponse {
        let body = try jsonEncoder.encode([req])
        let data = try await request(endpoint: "loginOrRegister", body: body)
        let response = try jsonDecoder.decode(LoginResponse.self, from: data)
        return response
    }
    
    func getRSSList(from urls: [URL]) async throws -> [RSS] {
        let body = try jsonEncoder.encode(urls)
        let data = try await request(endpoint: "getRSSList", body: body)
        let response = try jsonDecoder.decode([RSS].self, from: data)
        return response
    }
    
    func saveRSSUrls(request req: SaveRSSUrlRequest) async throws  {
        let body = try jsonEncoder.encode([req])
        _ = try await request(endpoint: "saveRSSUrls", body: body)
    }
    
    func subscribe(request req: SubscribeRequest) async throws {
        let body = try jsonEncoder.encode([req])
        _ = try await request(endpoint: "subscribe", body: body)
    }
    
    func unsubscribe(request req: UnsubscribeRequest) async throws {
        let body = try jsonEncoder.encode( [req])
        _ = try await request(endpoint: "unsubscribe", body: body)
    }
}
