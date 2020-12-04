
// TODO: Find the official UpdateInfo type from "electron-builder" lib.
export interface UpdateInfo {
    files: UpdateFile[];
    path: string;
    releaseDate: string;
    releaseName: string;
    releaseNotes: string;
    sha512: string;
    version: string;
}

export interface UpdateFile {
    sha512: string;
    size: number;
    url: string;
}
