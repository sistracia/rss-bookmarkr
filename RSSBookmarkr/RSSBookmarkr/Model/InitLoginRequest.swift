import Foundation

struct InitLoginRequest {
    let sessionId: String
}

extension InitLoginRequest: Encodable {
    enum CodingKeys: String, CodingKey {
        case sessionId = "SessionId"
    }
    
    func encode(to encoder: Encoder) throws {
        var container = encoder.container(keyedBy: CodingKeys.self)
        try container.encode(sessionId, forKey: .sessionId)
    }
}
