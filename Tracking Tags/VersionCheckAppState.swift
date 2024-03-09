//
//  VersionCheckAppState.swift
//  Tracking Tags
//
//  Created by Andrew Forget on 2024-03-09.
//

import SwiftUI

struct VersionCheckAppState {
    private var isCheckingForUpdates: Binding<Bool>
    var isShowingVersionAvailableToast: Binding<Bool>
    var isShowingVersionRequiredToast: Binding<Bool>
    var versionCheckToast: Binding<VersionCheckToast>
    
    init(
        isCheckingForUpdates: Binding<Bool>,
        isShowingVersionAvailableToast: Binding<Bool>,
        isShowingVersionRequiredToast: Binding<Bool>,
        versionCheckToast: Binding<VersionCheckToast>) {
            self.isCheckingForUpdates = isCheckingForUpdates
            self.isShowingVersionAvailableToast = isShowingVersionAvailableToast
            self.isShowingVersionRequiredToast = isShowingVersionRequiredToast
            self.versionCheckToast = versionCheckToast
        }
    
    func checkForUpdates() {
        isCheckingForUpdates.wrappedValue = true
        Task {
            try? await checkForUpdatesAsync()
        }
    }
    
    func resetCheckingForUpdates() {
        isCheckingForUpdates.wrappedValue = false
    }
    
    private func checkForUpdatesAsync() async throws -> Void {
        do {
            // Check version from server manifest
            let versionManifestUrl = URL(string: "https://vero.andydragon.com/static/data/trackingtags/version.json")!
            let versionManifest = try await URLSession.shared.decode(VersionManifest.self, from: versionManifestUrl)
            if Bundle.main.releaseVersionOlder(than: versionManifest.macOS.current) {
                DispatchQueue.main.async {
                    withAnimation {
                        versionCheckToast.wrappedValue = VersionCheckToast(
                            appVersion: Bundle.main.releaseVersionNumberPretty,
                            currentVersion: versionManifest.macOS.current,
                            linkToCurrentVersion: versionManifest.macOS.link)
                        if versionManifest.macOS.vital {
                            isShowingVersionRequiredToast.wrappedValue.toggle()
                        } else {
                            isShowingVersionAvailableToast.wrappedValue.toggle()
                        }
                    }
                }
            } else {
                resetCheckingForUpdates()
            }
        } catch {
            // do nothing, the version check is not critical
            debugPrint(error.localizedDescription)
        }
    }
}
