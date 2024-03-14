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

struct ScriptsCatalog: Codable {
    var hubs: [String: [Page]]
}

struct Page: Codable {
    var id: String { name }
    let name: String
    let pageName: String?
}

struct LoadedPage: Codable, Identifiable {
    var id: String {
        if self.hub.isEmpty {
            return self.name
        }
        return "\(self.hub):\(self.name)"
    }
    let hub: String
    let name: String
    let pageName: String?
    var displayName: String {
        if hub == "other" {
            return name
        }
        return "\(hub)_\(name)"
    }
    
    static func from(hub: String, page: Page) -> LoadedPage {
        return LoadedPage(hub: hub, name: page.name, pageName: page.pageName)
    }
}
