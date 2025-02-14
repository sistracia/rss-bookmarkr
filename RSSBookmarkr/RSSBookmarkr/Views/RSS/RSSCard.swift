import SwiftUI

struct RSSCard: View {
    let rss: RSS
    var body: some View {
        VStack(alignment: .leading, spacing: 10) {
            Text(rss.title).font(.title2)
            Link(rss.originHost, destination: URL(string: rss.originHostUrl)!)
            Text("\(dateToString(date: rss.publishDate)) (\(rss.timeAgo))")
            Link("Read", destination: rss.link)
        }
        .frame(maxWidth:.infinity, alignment: .leading)
        .padding()
        .overlay(
            RoundedRectangle(cornerRadius: 6)
                .stroke(.gray, lineWidth: 2)
        )
    }
    
    func dateToString(date: Date) -> String {
        let dateFormatter = DateFormatter()
        dateFormatter.dateStyle = .full
        return dateFormatter.string(from: date)
    }
}

#Preview {
    RSSCard(rss: RSS.placeholder)
}
