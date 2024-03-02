//
//  Types.swift
//  Tracking Tags
//
//  Created by Andrew Forget on 2024-02-16.
//

import SwiftUI
import SwiftData

@Model
class TrackingTag: Identifiable {
    var id: UUID = UUID()
    var tag: String
    
    init(id: UUID, tag: String) {
        self.id = id
        self.tag = tag
    }
    
    init(_ tag: String) {
        self.tag = tag
    }
}

enum FocusedField: Hashable {
    case userName,
         page
}

struct PageCatalog: Codable {
    var pages: [Page]
    var hubs: [String: [HubPage]]?
}

struct Page: Codable {
    var id: String { name }
    let name: String
    let pageName: String?
}

struct HubPage: Codable {
    var id: String { name }
    let name: String
    let pageName: String?
    let users: [String]?
}

struct LoadedPage: Codable, Identifiable {
    var id: String {
        if let hub = self.hubName {
            return "\(hub):\(self.name)"
        }
        return self.name
    }
    let name: String
    let pageName: String?
    let hubName: String?
    var displayName: String {
        if let hub = hubName {
            return "\(hub)_\(name)"
        }
        return name;
    }

    static func from(page: Page) -> LoadedPage {
        return LoadedPage(name: page.name, pageName: page.pageName, hubName: nil)
    }
    
    static func from(hubPage: HubPage, with name: String) -> LoadedPage {
        return LoadedPage(name: hubPage.name, pageName: hubPage.pageName, hubName: name)
    }
}


struct VersionManifest: Codable {
    let macOS: VersionEntry
    //let windows: VersionEntry
}

struct VersionEntry: Codable {
    let current: String
    let link: String
    let vital: Bool
}
