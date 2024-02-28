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

export const deploymentWebLocation = "/app/trackingtags";

export const versionLocation = "trackingtags/version.json";

export const macDmgLocation = "trackingtags/macos/Tracking%20Tags%20";
export const macReleaseNotesLocation = "releaseNotes-mac.json";

export const windowsInstallerLocation = "trackingtags/windows";
export const windowsReleaseNotesLocation = "releaseNotes-windows.json";

export type Platform = "macOS" | "windows";

export const platformString: Record<Platform, string> = {
    macOS: "macOS",
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
