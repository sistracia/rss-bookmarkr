import Foundation

struct UnsubscribeRequest{
    let email: String
}

extension UnsubscribeRequest: Encodable {
    enum CodingKeys: String, CodingKey {
        case email = "Email"
    }
    
    func encode(to encoder: any Encoder) throws {
        var container = encoder.container(keyedBy: CodingKeys.self)
        try container.encode(email, forKey: .email)
    }
}
