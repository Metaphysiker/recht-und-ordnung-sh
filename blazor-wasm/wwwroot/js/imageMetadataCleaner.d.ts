interface ExifData {
    [key: string]: any;
}

interface ImageMetadata {
    width: number;
    height: number;
    sizeWithMetadata: number;
    sizeWithoutMetadata: number;
    hasMetadata: boolean;
    exifData: ExifData;
    exifCount: number;
}

interface ImageComparisonMetadata {
    original: ImageMetadata;
    cleaned: ImageMetadata;
}

interface ImageMetadataCleaner {
    processedImages: Record<string, Blob>;
    imageMetadata: Record<string, ImageComparisonMetadata>;
    extractMetadata(dataUrl: string): Promise<ImageMetadata>;
    processImage(dataUrl: string, fileName: string): Promise<ImageComparisonMetadata>;
    formatBytes(bytes: number): string;
    downloadImage(fileName: string): void;
    clear(): void;
}

interface Window {
    imageMetadataCleaner: ImageMetadataCleaner;
    EXIF?: EXIF;
}
