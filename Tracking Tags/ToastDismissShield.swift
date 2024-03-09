//
//  ToastDismissShield.swift
//  Tracking Tags
//
//  Created by Andrew Forget on 2024-03-09.
//

import SwiftUI

struct ToastDismissShield: View {
    let isAnyToastShowing: Bool
    @Binding var isShowingToast: Bool
    @Binding var isShowingVersionAvailableToast: Bool
    
    var body: some View {
        if isAnyToastShowing {
            VStack {
                Rectangle().opacity(0.0000001)
            }
            .onTapGesture {
                if isShowingToast {
                    isShowingToast.toggle()
                } else if isShowingVersionAvailableToast {
                    isShowingVersionAvailableToast.toggle()
                }
            }
        }
    }
}

#Preview {
    let isAnyToastShowing: Bool = false
    @State var isShowingToast: Bool = false
    @State var isShowingVersionAvailableToast: Bool = false
    return ToastDismissShield(
        isAnyToastShowing: isAnyToastShowing,
        isShowingToast: $isShowingToast,
        isShowingVersionAvailableToast: $isShowingVersionAvailableToast)
}
