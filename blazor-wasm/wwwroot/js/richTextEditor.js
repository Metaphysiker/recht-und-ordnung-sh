window.RichTextEditor = {
    editors: {},

    initialize: function (elementId, dotNetRef, initialValue) {
        const quill = new Quill('#' + elementId, {
            theme: 'snow',
            modules: {
                toolbar: [
                    ['bold', 'italic', 'underline', 'strike'],
                    [{ 'list': 'ordered' }, { 'list': 'bullet' }],
                    [{ 'header': [1, 2, 3, false] }],
                    ['clean']
                ]
            }
        });

        if (initialValue) {
            quill.root.innerHTML = initialValue;
        }

        quill.on('text-change', function () {
            const html = quill.root.innerHTML === '<p><br></p>' ? '' : quill.root.innerHTML;
            dotNetRef.invokeMethodAsync('OnContentChanged', html);
        });

        this.editors[elementId] = quill;
    },

    destroy: function (elementId) {
        delete this.editors[elementId];
    }
};
