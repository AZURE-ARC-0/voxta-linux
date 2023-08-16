document.addEventListener("DOMContentLoaded", function() {
    function registerServiceTypeList(serviceTypeList) {

        let draggedElement = null;
        const placeholder = document.createElement('div');
        placeholder.className = 'service-ordering-item alert placeholder me-2';
        placeholder.style.opacity = '0.5';
        placeholder.style.width = '20px';

        serviceTypeList.addEventListener('dragover', ev => {
            ev.preventDefault();
            const target = ev.target.closest('.service-ordering-item');
            if (target === placeholder || target === serviceTypeList) return;

            const rect = target.getBoundingClientRect();
            const mid = (rect.left + rect.right) / 2;
            if (ev.clientX < mid) {
                target.before(placeholder);
            } else {
                target.after(placeholder);
            }
        });

        serviceTypeList.addEventListener('drop', ev => {
            ev.preventDefault();
            placeholder.replaceWith(draggedElement);

            const serviceType = serviceTypeList.dataset.servicetype;
            const orderedList = [];
            for (let i = 0; i < serviceTypeList.children.length; i++) {
                orderedList.push(serviceTypeList.children[i].dataset.servicelink);
            }
            const orderedServices = orderedList.join(',');

            const xhr = new XMLHttpRequest();
            xhr.open('POST', '/settings/reorder', true);
            xhr.setRequestHeader('Content-Type', 'application/x-www-form-urlencoded');
            xhr.send('serviceType=' + serviceType + '&orderedServices=' + orderedServices);

            xhr.onreadystatechange = function () {
                if (xhr.readyState === 4) {
                    console.log('Order changed: ', xhr.status)
                }
            };
        });

        const serviceLinks = serviceTypeList.getElementsByClassName('service-ordering-item');
        for (let serviceLinkIndex = 0; serviceLinkIndex < serviceLinks.length; serviceLinkIndex++) {
            const serviceLink = serviceLinks[serviceLinkIndex];
            
            serviceLink.addEventListener('dragstart', ev => {
                draggedElement = serviceLink;
                ev.dataTransfer.setData("text", ev.target.id);
                placeholder.style.height = `${serviceLink.offsetHeight}px`;
            });

            serviceLink.addEventListener('dragend', () => {
                draggedElement = null;
                placeholder.remove();
            });
        }
    }

    const serviceTypeLists = document.getElementsByClassName('service-ordering');
    for(let serviceTypeIndex = 0; serviceTypeIndex < serviceTypeLists.length; serviceTypeIndex++) {
        const serviceTypeList = serviceTypeLists[serviceTypeIndex];
        registerServiceTypeList(serviceTypeList);
    }
});