export const applicationName = "Tracking Tags";
export const applicationDescription = "Tracking is a small utility app for producing snap tracking tags.";
export const applicationDetails = (
    <>
        This utility lets you enter a user name and pick a snap page and it will generate the four snap tracking tags for the
        user. This includes:
        <br /><br />
        <div style={{marginLeft: "40px"}}>&#x27A4;&nbsp;&nbsp;snap_<em>pagename</em>_<em>username</em></div>
        <div style={{marginLeft: "40px"}}>&#x27A4;&nbsp;&nbsp;raw_<em>pagename</em>_<em>username</em></div>
        <div style={{marginLeft: "40px"}}>&#x27A4;&nbsp;&nbsp;snap_featured_<em>username</em></div>
        <div style={{marginLeft: "40px"}}>&#x27A4;&nbsp;&nbsp;raw_featured_<em>username</em></div>
        <br />
        These tags are used to track features for a user as part of featuring their photos as well as determining membership
        levels.
    </>
);
export const macScreenshotWidth = 720;
export const macScreenshotHeight = 400;
export const windowsScreenshotWidth = 620;
export const windowsScreenshotHeight = 310;

export const deploymentWebLocation = "/app/trackingtags";

export const versionLocation = "trackingtags/version.json";

export const showMacInfo = true;
export const macDmgLocation = "trackingtags/macos/Tracking%20Tags%20";
export const macReleaseNotesLocation = "releaseNotes-mac.json";

export const showMacV2Info = false;
export const macV2DmgLocation = "trackingtags/macos/Tracking%20Tags%20";
export const macV2ReleaseNotesLocation = "releaseNotes-mac_v2.json";

export const showWindowsInfo = true;
export const windowsInstallerLocation = "trackingtags/windows";
export const windowsReleaseNotesLocation = "releaseNotes-windows.json";

export const hasTutorial = false;

export type Platform = "macOS" | "macOS_v2" | "windows";

export const platformString: Record<Platform, string> = {
    macOS: "macOS",
    macOS_v2: "macOS v2",
    windows: "Windows"
}

export interface Links {
    readonly location: (version: string, flavorSuffix: string) => string;
    readonly actions: {
        readonly name: string;
        readonly action: string;
        readonly target: string;
        readonly suffix: string;
    }[];
}

export const links: Record<Platform, Links | undefined> = {
    macOS: {
        location: (version, suffix) => `${macDmgLocation}${suffix}v${version}.dmg`,
        actions: [
            {
                name: "default",
                action: "download",
                target: "",
                suffix: "",
            }
        ]
    },
    macOS_v2: {
        location: (version, suffix) => `${macDmgLocation}${suffix}v${version}.dmg`,
        actions: [
            {
                name: "default",
                action: "download",
                target: "",
                suffix: "",
            }
        ]
    },
    windows: {
        location: (_version, suffix) => `${windowsInstallerLocation}${suffix}`,
        actions: [
            {
                name: "current",
                action: "install",
                target: "",
                suffix: "/setup.exe",
            },
            {
                name: "current",
                action: "read more about",
                target: "_blank",
                suffix: "",
            }
        ]
    },
};

export interface NextStep {
    readonly label: string;
    readonly page: string;
}

export interface Screenshot {
    readonly name: string;
    readonly width?: string;
}

export interface Bullet {
    readonly text: string;
    readonly image?: Screenshot;
    readonly screenshot?: Screenshot;
    readonly link?: string;
}

export interface PageStep {
    readonly screenshot: Screenshot;
    readonly title: string;
    readonly bullets: Bullet[];
    readonly previousStep?: string;
    readonly nextSteps: NextStep[];
}

export const tutorialPages: Record<string, PageStep> = {
};
