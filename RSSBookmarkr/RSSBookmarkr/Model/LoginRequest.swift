import Foundation

struct LoginRequest {
    let username: String
    let password: String
}

extension LoginRequest: Encodable {
    enum CodingKeys: String, CodingKey {
        case username = "Username"
        case password = "Password"
    }
    
    func encode(to encoder: any Encoder) throws {
        var container = encoder.container(keyedBy: CodingKeys.self)
        try container.encode(username, forKey: .username)
        try container.encode(password, forKey: .password)
    }
}
