//
//  ContentView.swift
//  Tracking Tags
//
//  Created by Andrew Forget on 2024-02-16.
//

import SwiftUI
import AlertToast

struct ContentView: View {
    @Environment(\.openURL) var openURL
    @State var userName: String = ""
    @State var page: String = UserDefaults.standard.string(forKey: "Page") ?? "default"
    @State var trackingTags: [TrackingTag] = []
    @State var selectedTrackingTag: TrackingTag? = nil
    @State private var toastType: AlertToast.AlertType = .regular
    @State private var toastText = ""
    @State private var toastSubTitle = ""
    @State private var toastDuration = 3.0
    @State private var isShowingToast = false
    @FocusState var focusedField: FocusedField?
    @State var loadedPages = [LoadedPage]()
    var appState: VersionCheckAppState
    private var isAnyToastShowing: Bool {
        isShowingToast || appState.isShowingVersionAvailableToast.wrappedValue || appState.isShowingVersionRequiredToast.wrappedValue
    }

    init(_ appState: VersionCheckAppState) {
        self.appState = appState
    }

    var body: some View {
        ZStack {
            VStack {
                HStack {
                    Text("User: ")
                    TextField("Enter user name without '@'", text: $userName.onChange(updateTrackingTags))
                        .focused($focusedField, equals: .userName)
                    Button(action: {
                        userName = pasteFromClipboard()
                        updateTrackingTags(to: userName)
                    }) {
                        HStack {
                            Image(systemName: "clipboard")
                                .frame(alignment: .center)
                            Text("Paste")
                                .frame(alignment: .center)
                        }
                    }
                }
                HStack {
                    Text("Page: ")
                    Picker("", selection: $page.onChange(updateTrackingTags)) {
                        ForEach(loadedPages) { page in
                            if page.name != "default" {
                                Text(page.displayName).tag(page.id)
                            }
                        }
                    }
                    .focusable()
                    .focused($focusedField, equals: .page)
                }
                List(selection: $selectedTrackingTag) {
                    ForEach(trackingTags, id: \.self) { trackingTag in
                        HStack {
                            Text(trackingTag.tag)
                                .frame(alignment: .center)
                            Spacer()
                            Button(action: {
                                copyToClipboard(trackingTag.tag)
                                showToast("Copied!", "Copied \(trackingTag.tag) to the clipboard")
                            }) {
                                HStack {
                                    Image(systemName: "clipboard")
                                        .frame(alignment: .center)
                                    Text("Copy")
                                        .frame(alignment: .center)
                                }
                            }
                        }
                        .padding([.top], 8)
                        .onTapGesture {
                            selectedTrackingTag = trackingTag
                        }
                    }
                }
            }
            .padding()
            .allowsHitTesting(!isAnyToastShowing)
            if isAnyToastShowing {
                VStack {
                    Rectangle().opacity(0.0000001)
                }
                .onTapGesture {
                    if isShowingToast {
                        isShowingToast.toggle()
                    } else if appState.isShowingVersionAvailableToast.wrappedValue {
                        appState.isShowingVersionAvailableToast.wrappedValue.toggle()
                    }
                }
            }
        }
        .blur(radius: isAnyToastShowing ? 4 : 0)
        .toast(
            isPresenting: $isShowingToast,
            duration: 1,
            tapToDismiss: true,
            offsetY: 12,
            alert: {
                AlertToast(
                    displayMode: .hud,
                    type: toastType,
                    title: toastText,
                    subTitle: toastSubTitle)
            },
            completion: {
                focusedField = .userName
            })
        .toast(
            isPresenting: appState.isShowingVersionAvailableToast,
            duration: 10,
            tapToDismiss: true,
            offsetY: 12,
            alert: {
                AlertToast(
                    displayMode: .hud,
                    type: .systemImage("exclamationmark.triangle.fill", .yellow),
                    title: "New version available",
                    subTitle: "You are using v\(appState.versionCheckToast.wrappedValue.appVersion) and v\(appState.versionCheckToast.wrappedValue.currentVersion) is available\(appState.versionCheckToast.wrappedValue.linkToCurrentVersion.isEmpty ? "" : ", click here to open your browser") (this will go away in 10 seconds)")
            },
            onTap: {
                if let url = URL(string: appState.versionCheckToast.wrappedValue.linkToCurrentVersion) {
                    openURL(url)
                }
            },
            completion: {
                appState.resetCheckingForUpdates()
                focusedField = .userName
            })
        .toast(
            isPresenting: appState.isShowingVersionRequiredToast,
            duration: 0,
            tapToDismiss: true,
            offsetY: 12,
            alert: {
                AlertToast(
                    displayMode: .hud,
                    type: .systemImage("xmark.octagon.fill", .red),
                    title: "New version required",
                    subTitle: "You are using v\(appState.versionCheckToast.wrappedValue.appVersion) and v\(appState.versionCheckToast.wrappedValue.currentVersion) is required\(appState.versionCheckToast.wrappedValue.linkToCurrentVersion.isEmpty ? "" : ", click here to open your browser") or ⌘ + Q to Quit")
            },
            onTap: {
                if let url = URL(string: appState.versionCheckToast.wrappedValue.linkToCurrentVersion) {
                    openURL(url)
                    NSApplication.shared.terminate(nil)
                }
            },
            completion: {
                appState.resetCheckingForUpdates()
                focusedField = .userName
            })        
        .onAppear {
            focusedField = .userName
        }
        .task {
            do {
                let pagesUrl = URL(string: "https://vero.andydragon.com/static/data/pages.json")!
                let pagesCatalog = try await URLSession.shared.decode(PageCatalog.self, from: pagesUrl)
                var pages = [LoadedPage]()
                pages.append(contentsOf: pagesCatalog.pages.map({ LoadedPage.from(page: $0) }))
                pages.append(LoadedPage(name: "community", pageName: "community", hubName: nil))
                for hubPair in (pagesCatalog.hubs ?? [:]) {
                    for hubPage in hubPair.value {
                        if let pageUsers = hubPage.users {
                            if pageUsers.includesWithoutCase(ProcessInfo.processInfo.fullUserName) {
                                pages.append(LoadedPage.from(hubPage: hubPage, with: hubPair.key))
                            }
                        } else {
                            pages.append(LoadedPage.from(hubPage: hubPage, with: hubPair.key))
                        }
                    }
                }
                loadedPages.removeAll()
                loadedPages.append(contentsOf: pages.sorted(by: {
                    if $0.name.starts(with: "_") && $1.name.starts(with: "_") {
                        return $0.name < $1.name
                    }
                    if $0.name.starts(with: "_") {
                        return false
                    }
                    if $1.name.starts(with: "_") {
                        return true
                    }
                    return "\($0.hubName ?? "snap")_\($0.name)" < "\($1.hubName ?? "snap")_\($1.name)"
                }))

                do {
                    // Delay the start of the disallowed list download so the window can be ready faster
                    try await Task.sleep(nanoseconds: 100_000_000)
                    
                    appState.checkForUpdates()
                } catch {
                    // do nothing, the version check is not critical
                    debugPrint(error.localizedDescription)
                }
            } catch {
                debugPrint(error.localizedDescription)
            }
        }
    }

    private func updateTrackingTags(to value: String) {
        trackingTags.removeAll()
        UserDefaults.standard.set(page, forKey: "Page")
        if !page.isEmpty && !userName.isEmpty {
            let loadedPage = loadedPages.first(where: { pageEntry in pageEntry.id == page})
            var pageName = loadedPage?.pageName ?? loadedPage?.name ?? "unknown"
            if pageName.starts(with: "_") {
                pageName = String(pageName.dropFirst(1))
                trackingTags.append(TrackingTag("\(pageName)_\(userName)"))
            } else {
                trackingTags.append(TrackingTag("\(loadedPage?.hubName ?? "snap")_\(pageName)_\(userName)"))
                if loadedPage?.hubName == nil {
                    trackingTags.append(TrackingTag("raw_\(pageName)_\(userName)"))
                }
                trackingTags.append(TrackingTag("\(loadedPage?.hubName ?? "snap")_featured_\(userName)"))
                if loadedPage?.hubName == nil {
                    trackingTags.append(TrackingTag("raw_featured_\(userName)"))
                }
            }
        }
    }

    private func showToast(_ text: String, _ subTitle: String, duration: Double = 3.0) {
        toastType = .complete(.blue)
        toastText = text
        toastSubTitle = subTitle
        toastDuration = duration
        isShowingToast.toggle()
    }

    private func copyToClipboard(_ text: String) -> Void {
#if os(iOS)
        UIPasteboard.general.string = text
#else
        let pasteBoard = NSPasteboard.general
        pasteBoard.clearContents()
        pasteBoard.writeObjects([text as NSString])
#endif
    }
    
    private func pasteFromClipboard() -> String {
        let pasteBoard = NSPasteboard.general
        return pasteBoard.string(forType: .string) ?? ""
    }
}
