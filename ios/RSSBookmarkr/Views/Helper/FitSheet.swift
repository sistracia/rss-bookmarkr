import SwiftUI

struct FitSheet<SheetContent: View>: ViewModifier {
    @Binding var isPresented: Bool
    @State private var sheetHeight: CGFloat = .zero

    let onDismiss: (() -> Void)?
    let content: () -> SheetContent
    
    func body(content: Content) -> some View {
        content
            .sheet(isPresented: $isPresented, onDismiss: onDismiss) {
                self.content()
                    .heightChangePreference { height in
                        sheetHeight = height
                    }
                    .presentationDetents([.height(sheetHeight)])
            }
    }
}

extension View {
    func fitSheet<Content>(isPresented: Binding<Bool>, onDismiss: (() -> Void)? = nil, @ViewBuilder content: @escaping () -> Content) -> some View where Content : View {
        self.modifier(FitSheet(isPresented: isPresented, onDismiss: onDismiss, content: content))
    }
}
