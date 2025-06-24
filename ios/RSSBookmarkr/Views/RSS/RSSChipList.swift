import SwiftUI

struct RSSChipList: View {
    @Environment(ModelData.self) var modelData
    
    var body: some View {
        FlowLayout {
            ForEach(modelData.urls, id: \.absoluteString) {url in
                RSSChip(label: url.absoluteString) {
                    modelData.removeUrl(url: url)
                }
            }
        }
    }
}

#Preview {
    let modelData = ModelData(rssBookrmarkrClient: RSSBookmarkrClient(baseURL: URL(string: "https://rssbookmarkr.sistracia.com/rpc/IRPCStore/")!))
    modelData.urls = [
        URL(string: "https://a")!,
        URL(string: "https://b.com")!,
        URL(string: "https://overreacted.io/rss.xml")!,
        URL(string: "https://medium.com/feed/better-programming")!,
        URL(string: "https://blog.twitter.com/engineering/en_us/blog.rss")!,
        URL(string: "https://blog.twitter.com/engineering/en_us/blog.rsshttps://blog.twitter.com/engineering/en_us/blog.rsshttps://blog.twitter.com/engineering/en_us/blog.rss")!,
        URL(string: "https://b.com")!,
        URL(string: "https://b.com")!,
    ]
    return VStack {
        Text("Test")
        RSSChipList().environment(modelData)
        Text("Test")
    }
}
