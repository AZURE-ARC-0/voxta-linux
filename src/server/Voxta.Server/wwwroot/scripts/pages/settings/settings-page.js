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

        serviceTypeList.addEventListener('drop', async ev => {
            ev.preventDefault();
            placeholder.replaceWith(draggedElement);

            const serviceType = serviceTypeList.dataset.servicetype;
            const orderedList = [];
            for (let i = 0; i < serviceTypeList.children.length; i++) {
                orderedList.push(serviceTypeList.children[i].dataset.servicelink);
            }
            const orderedServices = orderedList.join(',');

            await fetch('/settings/reorder', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/x-www-form-urlencoded'
                },
                body: 'serviceType=' + serviceType + '&orderedServices=' + orderedServices
            });
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

            serviceLink.addEventListener('dblclick', async evt => {
                const selectedService = evt.target;
                const enabled = !selectedService.classList.contains('alert-primary');
                await fetch('/settings/enable', {
                    method: 'POST',
                    headers: {
                        'Content-Type': 'application/x-www-form-urlencoded'
                    },
                    body: 'serviceId=' + selectedService.dataset.serviceid + '&enabled=' + enabled
                });

                Array.from(document.getElementsByClassName('service-ordering-item')).forEach(element => {
                    if (element.dataset.servicename !== selectedService.dataset.servicename) return;
                    if (element.dataset.serviceid !== selectedService.dataset.serviceid) return;

                    if (enabled) {
                        element.classList.remove('alert-secondary');
                        element.classList.add('alert-primary');
                    } else {
                        element.classList.remove('alert-primary');
                        element.classList.add('alert-secondary');
                    }
                });
            });
        }
    }

    const serviceTypeLists = document.getElementsByClassName('service-ordering');
    for(let serviceTypeIndex = 0; serviceTypeIndex < serviceTypeLists.length; serviceTypeIndex++) {
        const serviceTypeList = serviceTypeLists[serviceTypeIndex];
        registerServiceTypeList(serviceTypeList);
    }
});