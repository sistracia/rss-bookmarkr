import SwiftUI

// Ref: https://stackoverflow.com/a/76687858/29628503
struct FlowLayout: Layout {
    var spacing: CGFloat = 8
    
    func sizeThatFits(proposal: ProposedViewSize, subviews: Subviews, cache: inout ()) -> CGSize {
        let result = FlowResult(
            proposal: proposal,
            in: proposal.replacingUnspecifiedDimensions().width,
            subviews: subviews,
            spacing: spacing
        )
        return CGSize(width: proposal.width ?? result.size.width,
                      height: result.size.height)
    }
    
    func placeSubviews(in bounds: CGRect, proposal: ProposedViewSize, subviews: Subviews, cache: inout ()) {
        let result = FlowResult(
            proposal: proposal,
            in: bounds.width,
            subviews: subviews,
            spacing: spacing
        )
        
        for (index, position) in result.positions.enumerated() {
            subviews[index].place(
                at: CGPoint(x: bounds.minX + position.x,
                            y: bounds.minY + position.y),
                proposal: ProposedViewSize(width: result.sizes[index].width,
                                           height: result.sizes[index].height)
            )
        }
    }
    
    private struct FlowResult {
        var positions: [CGPoint]
        var sizes: [CGSize]
        var size: CGSize
        
        init(proposal: ProposedViewSize, in maxWidth: CGFloat, subviews: Subviews, spacing: CGFloat) {
            positions = []
            sizes = []
            size = .zero
            
            var currentX: CGFloat = 0
            var currentY: CGFloat = 0
            var lineHeight: CGFloat = 0
            
            for subview in subviews {
                let viewSize = subview.sizeThatFits(.unspecified)
                
                if currentX + viewSize.width > maxWidth {
                    // Move to next line
                    currentX = 0
                    currentY += lineHeight + spacing
                    lineHeight = 0
                }
                
                positions.append(CGPoint(x: currentX, y: currentY))
                
                let height = subview.dimensions(in: proposal).height
                let width = subview.dimensions(in: proposal).width
                sizes.append(CGSize(width: width, height: height))
                
                lineHeight = max(lineHeight, height)
                currentX += width + spacing
                size.width = max(size.width, currentX)
            }
            
            size.height = currentY + lineHeight
        }
    }
}

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
    let modelData = ModelData()
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
