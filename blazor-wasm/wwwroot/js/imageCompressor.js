window.ImageCompressor = {
    compress: function (imageBytes, mimeType, maxWidth, maxHeight, quality) {
        return new Promise((resolve, reject) => {
            const blob = new Blob([new Uint8Array(imageBytes)], { type: mimeType });
            const url = URL.createObjectURL(blob);
            const img = new Image();

            img.onerror = () => { URL.revokeObjectURL(url); reject('Failed to load image'); };

            img.onload = function () {
                URL.revokeObjectURL(url);

                let { width, height } = img;
                if (width > maxWidth || height > maxHeight) {
                    const ratio = Math.min(maxWidth / width, maxHeight / height);
                    width = Math.floor(width * ratio);
                    height = Math.floor(height * ratio);
                }

                const canvas = document.createElement('canvas');
                canvas.width = width;
                canvas.height = height;
                canvas.getContext('2d').drawImage(img, 0, 0, width, height);

                const outputMime = mimeType === 'image/png' ? 'image/png' : 'image/jpeg';
                canvas.toBlob(compressed => {
                    const reader = new FileReader();
                    reader.onloadend = () => {
                        const bytes = new Uint8Array(reader.result);
                        let binary = '';
                        for (let i = 0; i < bytes.length; i++) binary += String.fromCharCode(bytes[i]);
                        resolve({ base64: btoa(binary), mimeType: outputMime });
                    };
                    reader.readAsArrayBuffer(compressed);
                }, outputMime, quality);
            };

            img.src = url;
        });
    }
};
