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
    let pages: [Page]
}

struct Page: Codable, Identifiable {
    var id: String { self.name }
    let name: String
    let pageName: String?
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
