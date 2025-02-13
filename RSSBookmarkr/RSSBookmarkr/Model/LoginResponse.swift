import Foundation

struct LoginSuccess {
    let userId: String
    let rssUrls: [String]
    let sessionId: String
    let email: String
}

extension LoginSuccess: Decodable {
    enum CodingKeys: String, CodingKey {
        case userId = "UserId"
        case rssUrls = "RssUrls"
        case sessionId = "SessionId"
        case email = "Email"
    }
}

struct LoginFailed {
    let message: String
}

extension LoginFailed: Decodable {
    enum CodingKeys: String, CodingKey {
        case message = "Message"
    }
}

enum LoginResponse {
    case success(LoginSuccess)
    case failed(LoginFailed)
}


extension LoginResponse: Decodable {
    enum CodingKeys: String, CodingKey {
        case success = "Success"
        case failed = "Failed"
    }
    
    init(from decoder: any Decoder) throws {
        let values = try decoder.container(keyedBy: CodingKeys.self)
        if let successData = try? values.decode(LoginSuccess.self, forKey: .success) {
            self = .success(successData)
        } else if let failedData = try? values.decode(LoginFailed.self, forKey: .failed) {
            self = .failed(failedData)
        } else {
            throw DecodingError.dataCorrupted(DecodingError.Context(codingPath: decoder.codingPath, debugDescription: "Invalid LoginResponse format"))
        }
    }
}
