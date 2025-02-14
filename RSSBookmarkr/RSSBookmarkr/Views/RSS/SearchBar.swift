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
                    modelData.addUrl(URL(string: url)!)
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
    @Previewable @State var modelData = ModelData()
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
