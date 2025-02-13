import Foundation

struct RSS  {
    let origin: String
    let title: String
    let publishDate: String
    let timeAgo: String
    let link: URL
    let originHost: String
    let originHostUrl: String
}

extension RSS: Identifiable {
    var id: String { link.path() }
}

extension RSS: Decodable {
    enum CodingKeys: String, CodingKey {
        case origin = "Origin"
        case title = "Title"
        case publishDate = "PublishDate"
        case timeAgo = "TimeAgo"
        case link = "Link"
        case originHost = "OriginHost"
        case originHostUrl = "OriginHostUrl"
    }
    
    init(from decoder: Decoder) throws {
        let values = try decoder.container(keyedBy: CodingKeys.self)
        let rawOrigin = try? values.decode(String.self, forKey: .origin)
        let rawTitle = try? values.decode(String.self, forKey: .title)
        let rawPublishDate = try? values.decode(String.self, forKey: .publishDate)
        let rawTimeAgo = try? values.decode(String.self, forKey: .timeAgo)
        let rawLink = try? values.decode(URL.self, forKey: .link)
        let rawOriginHost = try? values.decode(String.self, forKey: .originHost)
        let rawOriginHostUrl = try? values.decode(String.self, forKey: .originHostUrl)
        
        guard let origin = rawOrigin,
              let title = rawTitle,
              let publishDate = rawPublishDate,
              let timeAgo = rawTimeAgo,
              let link = rawLink,
              let originHost = rawOriginHost,
              let originHostUrl = rawOriginHostUrl
        else {
            throw RSSError.missingData
        }
        
        self.origin = origin
        self.title = title
        self.publishDate = publishDate
        self.timeAgo = timeAgo
        self.link = link
        self.originHost = originHost
        self.originHostUrl = originHostUrl
    }
}
