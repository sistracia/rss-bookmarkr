import SwiftUI

struct RSSChip: View {
    var label: String
    var onDelete: (() -> Void)
    var body: some View {
        HStack(alignment: .top) {
            Button(
                role: .destructive,
                action: onDelete,
                label: {
                    Label("Delete Button", systemImage: "xmark")
                        .labelStyle(.iconOnly)
                }
            )
            .padding(5)
            .foregroundStyle(.foreground)
            .background(.red)
            .clipShape(.buttonBorder)
            
            Text(label)
                .foregroundStyle(.background)
        }
        .padding(.vertical, 2)
        .padding(.horizontal, 4)
        .background(.foreground)
        .clipShape(.buttonBorder)
    }
}

#Preview {
    RSSChip(label:"https://blog.twitter.com/engineering/en_us/blog.rss") {}
}
