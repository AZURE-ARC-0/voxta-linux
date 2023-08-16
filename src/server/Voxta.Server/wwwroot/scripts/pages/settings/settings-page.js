document.addEventListener("DOMContentLoaded", function() {
    function registerServiceTypeList(serviceTypeList) {
        serviceTypeList.addEventListener('dragover', ev => ev.preventDefault());

        serviceTypeList.addEventListener('drop', ev => {
            ev.preventDefault();
            const data = ev.dataTransfer.getData("text");
            const el = document.getElementById(data);
            el.remove();
            ev.target.parentElement.appendChild(el);

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
                ev.dataTransfer.setData("text", ev.target.id);
            });
        }
    }

    const serviceTypeLists = document.getElementsByClassName('service-ordering');
    for(let serviceTypeIndex = 0; serviceTypeIndex < serviceTypeLists.length; serviceTypeIndex++) {
        const serviceTypeList = serviceTypeLists[serviceTypeIndex];
        registerServiceTypeList(serviceTypeList);
    }
});