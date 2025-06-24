import Foundation

struct SubscribeRequest {
    let userId: String
    let email: String
}

extension SubscribeRequest: Encodable {
    enum CodingKeys: String, CodingKey {
        case userId = "UserId"
        case email = "Email"
    }

    func encode(to encoder: any Encoder) throws {
        var container = encoder.container(keyedBy: CodingKeys.self)
        try container.encode(userId, forKey: .userId)
        try container.encode(email, forKey: .email)
    }
}
