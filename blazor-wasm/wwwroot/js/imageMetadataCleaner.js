window.imageMetadataCleaner = {
    // Store processed images and metadata in memory
    processedImages: {},
    imageMetadata: {},

    /**
     * Extract EXIF metadata from an image
     * @param {string} dataUrl - Base64 data URL of the image
     */
    extractMetadata: function(dataUrl) {
        return new Promise((resolve, reject) => {
            const img = new Image();

            img.onload = function() {
                // Get the data URL size (approximation of file size with metadata)
                const base64Length = dataUrl.split(',')[1].length;
                const sizeWithMetadata = Math.round((base64Length * 3) / 4);
                // Extract EXIF data if available
                /** @type {Record<string, any>} */
                let exifData = {};
                try {
                    if (typeof window.EXIF !== 'undefined' && window.EXIF.getData) {
                        window.EXIF.getData(img, function() {
                            // @ts-ignore - EXIF library sets 'this' to the image element
                            const allTags = window.EXIF.getAllTags(this);
                            
                            // Get ALL EXIF fields
                            exifData = { ...allTags };
                            
                            // Remove undefined/null values and internal properties
                            Object.keys(exifData).forEach((key) => {
                                if (exifData[key] === null || exifData[key] === undefined) {
                                    delete exifData[key];
                                }
                            });
                        });
                    }
                } catch (e) {
                    console.warn('Error extracting EXIF data:', e);
                }

                resolve({
                    width: img.width,
                    height: img.height,
                    sizeWithMetadata: sizeWithMetadata,
                    sizeWithoutMetadata: 0,
                    hasMetadata: Object.keys(exifData).length > 0,
                    exifData: exifData,
                    exifCount: Object.keys(exifData).length
                });
            };

            img.onerror = function() {
                reject(new Error('Failed to load image for metadata extraction'));
            };

            img.src = dataUrl;
        });
    },

    /**
     * Process an image to remove EXIF and other metadata
     * @param {string} dataUrl - Base64 data URL of the image
     * @param {string} fileName - Name of the file
     */
    processImage: function(dataUrl, fileName) {
        return new Promise(async (resolve, reject) => {
            try {
                // Extract metadata from original image
                const originalMetadata = await this.extractMetadata(dataUrl);

                const img = new Image();

                img.onload = function() {
                    try {
                        // Create a canvas element
                        const canvas = document.createElement('canvas');
                        const ctx = canvas.getContext('2d');

                        if (!ctx) {
                            reject(new Error('Failed to get canvas context'));
                            return;
                        }

                        // Set canvas dimensions to match image
                        canvas.width = img.width;
                        canvas.height = img.height;

                        // Draw the image on the canvas (this strips metadata)
                        ctx.drawImage(img, 0, 0);

                        // Convert canvas to blob (high quality)
                        canvas.toBlob(function(blob) {
                            if (!blob) {
                                reject(new Error('Failed to convert canvas to blob'));
                                return;
                            }

                            // Store the blob and metadata
                            window.imageMetadataCleaner.processedImages[fileName] = blob;
                            window.imageMetadataCleaner.imageMetadata[fileName] = {
                                original: originalMetadata,
                                cleaned: {
                                    width: img.width,
                                    height: img.height,
                                    sizeWithMetadata: 0,
                                    sizeWithoutMetadata: blob.size,
                                    hasMetadata: false,
                                    exifData: {},
                                    exifCount: 0
                                }
                            };

                            // Create object URLs for preview
                            const originalUrl = dataUrl;
                            const cleanedUrl = URL.createObjectURL(blob);

                            // Update preview images if they exist
                            const originalImg = document.getElementById('original-' + fileName);
                            if (originalImg instanceof HTMLImageElement) {
                                originalImg.src = originalUrl;
                            }

                            const cleanedImg = document.getElementById('cleaned-' + fileName);
                            if (cleanedImg instanceof HTMLImageElement) {
                                cleanedImg.src = cleanedUrl;
                            }

                            // Update metadata displays
                            const metadata = window.imageMetadataCleaner.imageMetadata[fileName];

                            const originalInfo = document.getElementById('original-info-' + fileName);
                            if (originalInfo) {
                                let exifHtml = '';
                                const exifData = metadata.original.exifData || {};
                                const exifCount = Object.keys(exifData).length;
                                
                                if (exifCount > 0) {
                                    exifHtml = '<div style="margin-top: 8px; padding: 8px; background: #fff3e0; border-radius: 4px;">';
                                    exifHtml += `<strong style="color: #f44336;">Gefunden ${exifCount} EXIF-Felder:</strong><br/>`;
                                    exifHtml += '<div style="font-size: 0.8rem; margin-top: 4px; max-height: 200px; overflow-y: auto;">';
                                    
                                    Object.keys(exifData).forEach(key => {
                                        let value = exifData[key];
                                        // Format certain values
                                        if (Array.isArray(value)) {
                                            value = value.join(', ');
                                        } else if (typeof value === 'object' && value !== null) {
                                            value = JSON.stringify(value);
                                        }
                                        exifHtml += `<div style="margin: 2px 0;"><strong>${key}:</strong> ${value}</div>`;
                                    });
                                    
                                    exifHtml += '</div></div>';
                                } else {
                                    exifHtml = '<div style="color: #666; font-style: italic; margin-top: 4px;">Keine EXIF-Daten gefunden</div>';
                                }
                                
                                originalInfo.innerHTML = `
                                    <strong>Original:</strong><br/>
                                    Abmessungen: ${metadata.original.width} × ${metadata.original.height}
                                    ${exifHtml}
                                `;
                            }

                            const cleanedInfo = document.getElementById('cleaned-info-' + fileName);
                            if (cleanedInfo) {
                                cleanedInfo.innerHTML = `
                                    <strong>Bereinigt:</strong><br/>
                                    Abmessungen: ${metadata.cleaned.width} × ${metadata.cleaned.height}<br/>
                                    <div style="margin-top: 8px; padding: 8px; background: #e8f5e9; border-radius: 4px;">
                                        <span style="color: #4caf50;">✓ Alle Metadaten entfernt</span>
                                    </div>
                                `;
                            }

                            resolve(metadata);
                        }, 'image/jpeg', 0.95); // Use JPEG with 95% quality

                    } catch (error) {
                        reject(error);
                    }
                };

                img.onerror = function() {
                    reject(new Error('Failed to load image'));
                };

                img.src = dataUrl;

            } catch (error) {
                reject(error);
            }
        });
    },

    /**
     * Format bytes to human-readable string
     * @param {number} bytes - Number of bytes
     * @returns {string} Formatted string
     */
    formatBytes: function(bytes) {
        if (bytes === 0) return '0 B';
        const k = 1024;
        const sizes = ['B', 'KB', 'MB', 'GB'];
        const i = Math.floor(Math.log(bytes) / Math.log(k));
        return parseFloat((bytes / Math.pow(k, i)).toFixed(2)) + ' ' + sizes[i];
    },

    /**
     * Download a processed image
     * @param {string} fileName - Name of the file to download
     */
    downloadImage: function(fileName) {
        const blob = this.processedImages[fileName];
        if (!blob) {
            console.error('Image not found:', fileName);
            return;
        }

        // Create a temporary anchor element to trigger download
        const url = URL.createObjectURL(blob);
        const a = document.createElement('a');
        a.href = url;

        // Add "-cleaned" suffix to the filename
        const nameParts = fileName.split('.');
        const extension = nameParts.pop();
        const baseName = nameParts.join('.');
        a.download = baseName + '-cleaned.' + extension;

        document.body.appendChild(a);
        a.click();
        document.body.removeChild(a);

        // Clean up the object URL after a short delay
        setTimeout(() => URL.revokeObjectURL(url), 100);
    },

    /**
     * Clear all processed images from memory
     */
    clear: function() {
        // Revoke all object URLs to free memory
        for (const fileName in this.processedImages) {
            const cleanedImg = document.getElementById('cleaned-' + fileName);
            if (cleanedImg instanceof HTMLImageElement && cleanedImg.src) {
                URL.revokeObjectURL(cleanedImg.src);
            }
        }
        this.processedImages = {};
        this.imageMetadata = {};
    }
};
