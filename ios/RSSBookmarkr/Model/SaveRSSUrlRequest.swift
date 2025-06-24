import Foundation

struct SaveRSSUrlRequest {
    let userId: String
    let urls: [URL]
}

extension SaveRSSUrlRequest: Encodable {
    enum CodingKeys: String, CodingKey {
        case userId = "UserId"
        case urls = "Urls"
    }
    
    func encode(to encoder: Encoder) throws {
        var container = encoder.container(keyedBy: CodingKeys.self)
        try container.encode(userId, forKey: .userId)
        try container.encode(urls, forKey: .urls)
    }
}
