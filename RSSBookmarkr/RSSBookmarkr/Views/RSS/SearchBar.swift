import SwiftUI

struct SearchBar: View {
    @Environment(ModelData.self) var modelData
    @State private var url: String = ""
    
    var body: some View {
        VStack {
            HStack {
                TextField(text: $url, prompt: Text(verbatim: "https://overreacted.io/rss.xml")) {
                    Text("Search URL")
                }
                .textInputAutocapitalization(.never)
                .labelsHidden()
                .padding()
                .clipShape(
                    RoundedRectangle(cornerRadius: 6)
                )
                .overlay(
                    RoundedRectangle(cornerRadius: 6)
                        .stroke(.gray, lineWidth: 2)
                )
                
                Button {
                    guard let parsedUrl = URL(string:url) else {
                        return
                    }
                    
                    modelData.addUrl(parsedUrl)
                    url = ""
                } label: {
                    Text("Add")
                        .padding()
                }
                .foregroundStyle(.background)
                .background(.foreground)
                .clipShape(.buttonBorder)
            }
            .padding(5)
            
            RSSChipList().padding(2)
        }
    }
}

#Preview {
    @Previewable @State var modelData = ModelData(rssBookrmarkrClient: RSSBookmarkrClient(baseURL: URL(string: "https://rssbookmarkr.sistracia.com/rpc/IRPCStore/")!))
    modelData.urls = [
        URL(string: "https://a")!,
        URL(string: "https://b.com")!,
        URL(string: "https://overreacted.io/rss.xml")!,
        URL(string: "https://medium.com/feed/better-programming")!,
        URL(string: "https://blog.twitter.com/engineering/en_us/blog.rss")!,
        URL(string: "https://a")!,
        URL(string: "https://a")!,
        URL(string: "https://a")!,
        URL(string: "https://b.com")!,
        URL(string: "https://overreacted.io/rss.xml")!,
        URL(string: "https://medium.com/feed/better-programming")!,
        URL(string: "https://blog.twitter.com/engineering/en_us/blog.rss")!,
        URL(string: "https://b.com")!,
        URL(string: "https://overreacted.io/rss.xml")!,
        URL(string: "https://medium.com/feed/better-programming")!,
        URL(string: "https://blog.twitter.com/engineering/en_us/blog.rss")!,
        URL(string: "https://b.com")!,
        URL(string: "https://overreacted.io/rss.xml")!,
        URL(string: "https://medium.com/feed/better-programming")!,
        URL(string: "https://blog.twitter.com/engineering/en_us/blog.rss")!,
        URL(string: "https://a")!,
        URL(string: "https://b.com")!,
        URL(string: "https://overreacted.io/rss.xml")!,
        URL(string: "https://medium.com/feed/better-programming")!,
        URL(string: "https://blog.twitter.com/engineering/en_us/blog.rss")!,
    ]
    return SearchBar().environment(modelData)
}
