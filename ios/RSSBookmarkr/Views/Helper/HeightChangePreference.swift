
import SwiftUI

struct GetHeightModifier: ViewModifier {
    let completion: (CGFloat) -> ()
    
    init(completion: @escaping (CGFloat) -> Void) {
        self.completion = completion
    }
    
    func body(content: Content) -> some View {
        content.overlay {
            GeometryReader { geometry in
                return Color.clear.preference(key: InnerHeightPreferenceKey.self, value: geometry.size.height)
            }
        }
        .onPreferenceChange(InnerHeightPreferenceKey.self) { newHeight in
            completion(newHeight)
        }
    }
}

public extension View {
    func heightChangePreference(completion: @escaping (CGFloat) -> ()) -> some View {
        return modifier(GetHeightModifier(completion: completion))
    }
}

struct InnerHeightPreferenceKey: PreferenceKey {
    static let defaultValue: CGFloat = .zero
    static func reduce(value: inout CGFloat, nextValue: () -> CGFloat) {
        value = nextValue()
    }
}
