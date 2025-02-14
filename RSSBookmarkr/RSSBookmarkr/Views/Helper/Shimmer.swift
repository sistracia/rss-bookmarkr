// Ref: https://github.com/joshuajhomann/Shimmer
// Ref: https://joshhomann.medium.com/generic-shimmer-loading-skeletons-in-swiftui-26fcd93ccee5

import SwiftUI

public struct ShimmerConfiguration {
    public let gradient: Gradient
    public let initialLocation: (start: UnitPoint, end: UnitPoint)
    public let finalLocation: (start: UnitPoint, end: UnitPoint)
    public let duration: TimeInterval
    public let opacity: Double
    public static let `default` = ShimmerConfiguration(
        gradient: Gradient(stops: [
            .init(color: .black, location: 0),
            .init(color: .white, location: 0.3),
            .init(color: .white, location: 0.7),
            .init(color: .black, location: 1),
        ]),
        initialLocation: (start: UnitPoint(x: -1, y: 0.5), end: .leading),
        finalLocation: (start: .trailing, end: UnitPoint(x: 2, y: 0.5)),
        duration: 2,
        opacity: 0.6
    )
}

struct ShimmeringView<Content: View>: View {
    private let configuration: ShimmerConfiguration
    private let show: Bool
    private let content: () -> Content
    @State private var startPoint: UnitPoint
    @State private var endPoint: UnitPoint
    init(configuration: ShimmerConfiguration, show: Bool, @ViewBuilder content: @escaping () -> Content) {
        self.configuration = configuration
        self.show = show
        self.content = content
        _startPoint = .init(wrappedValue: configuration.initialLocation.start)
        _endPoint = .init(wrappedValue: configuration.initialLocation.end)
    }
    var body: some View {
        ZStack {
            content()
            if show {
                LinearGradient(
                    gradient: configuration.gradient,
                    startPoint: startPoint,
                    endPoint: endPoint
                )
                .opacity(configuration.opacity)
                .blendMode(.screen)
                .onAppear {
                    withAnimation(Animation.linear(duration: configuration.duration).repeatForever(autoreverses: false)) {
                        startPoint = configuration.finalLocation.start
                        endPoint = configuration.finalLocation.end
                    }
                }            }
            
        }
    }
}

public struct ShimmerModifier: ViewModifier {
    let configuration: ShimmerConfiguration
    let show: Bool
    public func body(content: Content) -> some View {
        ShimmeringView(configuration: configuration, show: show) { content }
    }
}


public extension View {
    func shimmer(configuration: ShimmerConfiguration = .default, show: Bool = true) -> some View {
        modifier(ShimmerModifier(configuration: configuration, show: show))
    }
}

#Preview {
    struct PlaceholderView: View {
        var body: some View {
            ForEach (0..<10) { _ in
                HStack {
                    Image(systemName: "star.fill")
                    Text("Lorem ipsum dolor sit amet, consectetur adipiscing elit, sed do eiusmod tempor incididunt ut labore et dolore magna aliqua. Ut enim ad minim veniam")
                }
                .redacted(reason: .placeholder)
                .shimmer()
            }
        }
    }
    
    return PlaceholderView()
}
