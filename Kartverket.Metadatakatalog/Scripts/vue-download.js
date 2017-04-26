﻿
function showAlert(message, colorClass) {
    $('#feedback-alert').attr('class', 'alert alert-dismissible alert-' + colorClass);
    $('#feedback-alert .message').html($('#feedback-alert .message').html() + message + "<br/>");
    $('#feedback-alert').show();
}

function clearAlertMessage() {
    $('#feedback-alert .message').html("");
}

function hideAlert() {
    $('#feedback-alert').hide();
}

$(document).on('focus', '.custom-select-list-input', function () {
    var customSelectListElement = $(this).closest('.custom-select-list');
    var dropdownElement = customSelectListElement.find('.custom-select-list-dropdown');
    dropdownElement.addClass('active');
    dropdownElement.removeClass('transparent');
})

$(document).on('blur', '.custom-select-list-input', function () {
    var inputElement = this;
    var customSelectListElement = $(this).closest('.custom-select-list');
    var dropdownElement = customSelectListElement.find('.custom-select-list-dropdown');
    dropdownElement.addClass("transparent")
    setTimeout(function () {
        if (inputElement !== document.activeElement) {
            dropdownElement.removeClass("active")
            dropdownElement.removeClass("transparent")
        }
    }, 1000);
})

$(document).on('click', '.custom-select-list-input-container', function () {
    $(this).find('.custom-select-list-input').focus();
})

$(document).on("click", "#remove-all-items", function () {
    $('#remove-all-items-modal').modal('show')
});


function fixUrl(urlen) {
    urlJson = urlen.replace("%3F", "?");
    return urlJson;
}


function getOrderItemName(uuid) {
    var metadata = JSON.parse(localStorage.getItem(uuid + ".metadata"));
    return metadata !== null && metadata.name !== undefined ? metadata.name : "";
}

function IsGeonorge(distributionUrl) {
    return distributionUrl.indexOf("geonorge.no") > -1;
}

function getJsonData(url) {
    var lastSlash = url.lastIndexOf('/');
    var uuid = url.substring(lastSlash + 1);
    var name = getOrderItemName(uuid);
    var jsonUri = fixUrl(url);
    returnData = "";
    if (jsonUri != "") {
        $.ajax({
            url: jsonUri,
            dataType: 'json',
            async: false,
            error: function (jqXHR, textStatus, errorThrown) {
                showAlert("Vennligst fjern " + name + " fra kurv. Feilmelding: " + errorThrown + "<br/>", "danger");
            },
            success: function (data) {
                if (data !== null) {
                    returnData = data;
                }
                else {
                    showAlert("Data mangler for å kunne lastes ned. Vennligst fjern " + name + " fra kurv. <br/>", "danger");
                }
            }
        });
    }
    return returnData;
}


showLoadingAnimation("Laster inn kurv");

$(window).load(function () {
    hideLoadingAnimation();
});

var MapSelect = {
    props: ['mapData', 'mapSrc', 'master'],
    template: '#map-template',
}


var Areas = {
    props: ['available', 'selected', 'master'],
    template: '#areas-template',
    data: function () {
        var data = {
            supportsPolygonSelection: false,
            supportsGridSelection: false
        }
        if (!this.master) {
            data.supportsPolygonSelection = this.$parent.capabilities.supportsPolygonSelection,
            data.supportsGridSelection = this.$parent.capabilities.supportsGridSelection
        }
        return data;
    },

    methods: {
        selectArea: function (area) {
            area.isLocalSelected = true;
            if (this.master) {
                area.isSelected = true;
                for (orderLine in this.$parent.allAvailableAreas) {
                    if (this.$parent.allAvailableAreas[orderLine][area.type] !== undefined) {
                        this.$parent.allAvailableAreas[orderLine][area.type].forEach(function (availableArea) {
                            if (availableArea.code == area.code) {
                                availableArea.isSelected = true;
                                availableArea.isLocalSelected = true;
                            }
                        })
                    }
                }
            }

            this.$parent.updateSelectedAreas();

            this.$parent.updateAvailableProjections();
            this.$parent.updateAvailableFormats();

            this.$parent.updateSelectedProjections();
            this.$parent.updateSelectedFormats();

            this.$root.validateAreas();
        },
        removeSelectedArea: function (area) {
            area.isLocalSelected = false;
            if (this.master) {
                area.isSelected = false;
                for (orderLine in this.$parent.allAvailableAreas) {
                    if (this.$parent.allAvailableAreas[orderLine][area.type] !== undefined) {
                        this.$parent.allAvailableAreas[orderLine][area.type].forEach(function (availableArea) {
                            if (availableArea.code == area.code) {
                                availableArea.isSelected = false;
                                availableArea.isLocalSelected = false;
                            }
                        })
                    }
                }
            }

            this.$parent.updateSelectedAreas();

            this.$parent.updateAvailableProjections();
            this.$parent.updateAvailableFormats();

            this.$parent.updateSelectedProjections();
            this.$parent.updateSelectedFormats();

            this.$root.validateAreas();

        }
    },
    components: {
        'mapSelect': MapSelect
    }
};

var Projections = {
    props: ['available', 'selected', 'master'],
    template: '#projections-template',

    methods: {
        selectProjection: function (projection) {
            projection.isLocalSelected = true;
            if (this.master) {
                projection.isSelected = true;
                for (orderLine in this.$parent.allAvailableProjections) {
                    this.$parent.allAvailableProjections[orderLine].forEach(function (availableProjection) {
                        if (availableProjection.code == projection.code) {
                            availableProjection.isSelected = true;
                            availableProjection.isLocalSelected = true;
                        }
                    })
                }
            }
            this.$parent.updateSelectedProjections();
            this.$root.validateAreas();
            this.$parent.updateSelectedAreas();
        },
        removeSelectedProjection: function (projection) {
            projection.isLocalSelected = false;
            if (this.master) {
                projection.isSelected = false;
                for (orderLine in this.$parent.allAvailableProjections) {
                    this.$parent.allAvailableProjections[orderLine].forEach(function (availableProjection) {
                        if (availableProjection.code == projection.code) {
                            availableProjection.isSelected = false;
                            availableProjection.isLocalSelected = false;
                        }
                    })
                }
            }
            this.$parent.updateSelectedProjections();
            this.$root.validateAreas();
            this.$parent.updateSelectedAreas();
        }
    }
};

var Formats = {
    props: ['available', 'selected', 'master'],
    template: '#formats-template',

    methods: {
        selectFormat: function (format) {
            format.isLocalSelected = true;
            if (this.master) {
                format.isSelected = true;
                for (orderLine in this.$parent.allAvailableFormats) {
                    this.$parent.allAvailableFormats[orderLine].forEach(function (availableFormat) {
                        if (availableFormat.name == format.name) {
                            availableFormat.isSelected = true;
                            availableFormat.isLocalSelected = true;
                        }
                    })
                }
            }
            this.$parent.updateSelectedFormats();
            this.$root.validateAreas();
            this.$parent.updateSelectedAreas();
        },
        removeSelectedFormat: function (format) {
            format.isLocalSelected = false;
            if (this.master) {
                format.isSelected = false;
                for (orderLine in this.$parent.allAvailableFormats) {
                    this.$parent.allAvailableFormats[orderLine].forEach(function (availableFormat) {
                        if (availableFormat.name == format.name) {
                            availableFormat.isSelected = false;
                            availableFormat.isLocalSelected = false;
                        }
                    })
                }
            }
            this.$parent.updateSelectedFormats();
            this.$root.validateAreas();
            this.$parent.updateSelectedAreas();
        }
    }
};



var OrderLine = {
    props: ['metadata', 'capabilities', 'availableAreas', 'availableProjections', 'availableFormats', 'selectedAreas', 'selectedProjections', 'selectedFormats', 'selectedCoordinates', 'defaultProjections', 'defaultFormats', 'orderLineErrors'],
    template: '#order-line-template',
    data: function () {
        var data = {
            expanded: false,
            mapData: {},
            mapIsLoaded: false,
            showMap: false
        }
        return data;
    },
    computed: {
        hasErrors: function () {
            var hasErrors = false;
            if (this.orderLineErrors !== undefined && Object.keys(this.orderLineErrors).length) {
                for (errorType in this.orderLineErrors) {
                    if (this.orderLineErrors[errorType].length) {
                        hasErrors = true;
                    }
                }
            }
            return hasErrors;
        },
        numberOfErrors: function () {
            var numberOfErrors = 0;
            if (this.orderLineErrors !== undefined && Object.keys(this.orderLineErrors).length) {
                for (errorType in this.orderLineErrors) {
                    numberOfErrors += this.orderLineErrors[errorType].length;
                }
            }
            return numberOfErrors;
        }
    },
    methods: {
        isAllreadyAdded: function (array, item, propertyToCompare) {
            var isAllreadyAdded = {
                added: false,
                position: 0
            };
            if (array.length) {
                array.forEach(function (arrayItem, index) {
                    if (this.readProperty(arrayItem, propertyToCompare) == this.readProperty(item, propertyToCompare)) {
                        isAllreadyAdded.added = true
                        isAllreadyAdded.position = index;
                    };
                }.bind(this))
            }
            return isAllreadyAdded;
        },
        readProperty: function (obj, prop) {
            return obj[prop];
        },
        filterOptionList: function (optionListId, inputValue) {
            var dropdownListElements = document.getElementsByClassName(optionListId);
            var filter = inputValue.toUpperCase();
            for (var listIndex = 0; listIndex < dropdownListElements.length; listIndex++) {
                var listItems = dropdownListElements[listIndex].getElementsByTagName('li');
                var hasResults = false;
                for (var i = 0; i < listItems.length; i++) {
                    if (listItems[i].innerHTML.toUpperCase().indexOf(filter) > -1) {
                        listItems[i].style.display = "";
                        hasResults = true;
                    } else {
                        listItems[i].style.display = "none";
                    }
                }

                var optionGroupNameElement = $(dropdownListElements[listIndex]).closest("div").find(".custom-select-list-option-group-name");
                if (!hasResults) {
                    optionGroupNameElement.hide();
                } else {
                    optionGroupNameElement.show();
                }
            }
        },
        updateSelectedAreas: function () {
            var orderLineUuid = this.metadata.uuid;
            var selectedAreas = [];
            for (areaType in this.$parent.masterOrderLine.allAvailableAreas[orderLineUuid]) {
                if (this.$parent.masterOrderLine.allAvailableAreas[orderLineUuid][areaType].length) {
                    this.$parent.masterOrderLine.allAvailableAreas[orderLineUuid][areaType].forEach(function (localSelectedArea) {
                        if (localSelectedArea.isLocalSelected) {
                            var isAllreadyAddedInfo = this.isAllreadyAdded(selectedAreas, localSelectedArea, "code");
                            if (!isAllreadyAddedInfo.added) {
                                selectedAreas.push(localSelectedArea);
                            }
                        }
                    }.bind(this))
                }
            }
            this.$parent.masterOrderLine.allSelectedAreas[orderLineUuid] = selectedAreas;
        },
        updateAvailableProjections: function () {
            var orderLineUuid = this.metadata.uuid;
            var availableProjections = [];
            if (this.$parent.masterOrderLine.allSelectedAreas[orderLineUuid].length) {
                this.$parent.masterOrderLine.allSelectedAreas[orderLineUuid].forEach(function (selectedArea) {
                    if (selectedArea.allAvailableProjections[orderLineUuid] !== undefined && selectedArea.allAvailableProjections[orderLineUuid].length) {
                        selectedArea.allAvailableProjections[orderLineUuid].forEach(function (availableProjection) {
                            var isAllreadyAddedInfo = this.isAllreadyAdded(availableProjections, availableProjection, "code");
                            if (!isAllreadyAddedInfo.added) {
                                availableProjections.push(availableProjection);
                            }
                        }.bind(this))
                    }

                }.bind(this))
            }
            this.$parent.masterOrderLine.allAvailableProjections[orderLineUuid] = availableProjections;
        },
        updateSelectedProjections: function () {
            var orderLineUuid = this.metadata.uuid;
            var selectedProjections = [];
            if (this.$parent.masterOrderLine.allAvailableProjections[orderLineUuid] !== undefined && this.$parent.masterOrderLine.allAvailableProjections[orderLineUuid].length) {
                this.$parent.masterOrderLine.allAvailableProjections[orderLineUuid].forEach(function (localSelectedProjection) {
                    if (localSelectedProjection.isLocalSelected) {
                        var isAllreadyAddedInfo = this.isAllreadyAdded(selectedProjections, localSelectedProjection, "code");
                        if (!isAllreadyAddedInfo.added) {
                            selectedProjections.push(localSelectedProjection);
                        }

                    }
                }.bind(this))
            }

            this.$parent.masterOrderLine.allSelectedProjections[orderLineUuid] = selectedProjections;
        },
        updateAvailableFormats: function () {
            var orderLineUuid = this.metadata.uuid;
            var availableFormats = [];
            if (this.$parent.masterOrderLine.allSelectedAreas[orderLineUuid].length) {
                this.$parent.masterOrderLine.allSelectedAreas[orderLineUuid].forEach(function (selectedArea) {
                    if (selectedArea.allAvailableFormats[orderLineUuid] !== undefined && selectedArea.allAvailableFormats[orderLineUuid].length) {
                        selectedArea.allAvailableFormats[orderLineUuid].forEach(function (availableFormat) {
                            var isAllreadyAddedInfo = this.isAllreadyAdded(availableFormats, availableFormat, "name");
                            if (!isAllreadyAddedInfo.added) {
                                availableFormats.push(availableFormat);
                            }
                        }.bind(this))
                    }

                }.bind(this))
            }
            this.$parent.masterOrderLine.allAvailableFormats[orderLineUuid] = availableFormats;
        },
        updateSelectedFormats: function () {
            var orderLineUuid = this.metadata.uuid;
            var selectedFormats = [];
            if (this.$parent.masterOrderLine.allAvailableFormats[orderLineUuid] !== undefined && this.$parent.masterOrderLine.allAvailableFormats[orderLineUuid].length) {
                this.$parent.masterOrderLine.allAvailableFormats[orderLineUuid].forEach(function (localSelectedFormat) {
                    if (localSelectedFormat.isLocalSelected) {
                        var isAllreadyAddedInfo = this.isAllreadyAdded(selectedFormats, localSelectedFormat, "name");
                        if (!isAllreadyAddedInfo.added) {
                            selectedFormats.push(localSelectedFormat);
                        }
                    }
                }.bind(this))
            }

            this.$parent.masterOrderLine.allSelectedFormats[orderLineUuid] = selectedFormats;
        },
        selectFromMap: function (orderItem, mapType) {
            orderItem.showMap = true;
            var fixed = orderItem.capabilities.supportsGridSelection;
            if (mapType == "grid") { this.loadGridMap(orderItem) }
            else if (mapType == "polygon") { this.loadPolygonMap(orderItem) }
            $('#norgeskartmodal #setcoordinates').attr('uuid', orderItem.metadata.uuid);
        },

        isJson: function (str) {
            try {
                JSON.parse(str);
            } catch (e) {
                return false;
            }
            return true;
        },

        loadGridMap: function (orderItem) {
            orderItem.mapIsLoaded = true;
            orderItem.mapData.defaultConfigurations = {
                "service_name": orderItem.capabilities.mapSelectionLayer,
                "center_longitude": "378604",
                "center_latitude": "7226208",
                "zoom_level": "3"
            }

            window.addEventListener('message', function (e) {
                if (e !== undefined && e.data !== undefined && typeof (e.data) == "string") {
                    var msg = JSON.parse(e.data);
                    if (msg.type === "mapInitialized") {

                        var iframeElement = document.getElementById(orderItem.metadata.uuid + "-iframe").contentWindow;

                        iframeMessage = {
                            "cmd": "setCenter",
                            "x": orderItem.mapData.defaultConfigurations.center_longitude,
                            "y": orderItem.mapData.defaultConfigurations.center_latitude,
                            "zoom": orderItem.mapData.defaultConfigurations.zoom_level
                        };
                        iframeElement.postMessage(JSON.stringify(iframeMessage), '*');

                        iframeMessage = {
                            "cmd": "setVisible",
                            "id": orderItem.mapData.defaultConfigurations.service_name
                        };
                        iframeElement.postMessage(JSON.stringify(iframeMessage), '*');

                    } else {
                        if (msg.cmd === "setVisible") return;
                        var obj = msg;

                        if (this.isJson(msg)) {
                            var data = JSON.parse(msg);
                            if (data["type"] == "mapInitialized") return;

                            var areaname = data["attributes"]["n"];



                            if (data["cmd"] == "featureSelected") {
                                for (areaType in this.$root.masterOrderLine.allAvailableAreas[orderItem.metadata.uuid]) {
                                    this.$root.masterOrderLine.allAvailableAreas[orderItem.metadata.uuid][areaType].forEach(function (availableArea) {
                                        if (availableArea.code == areaname) {
                                            availableArea.isLocalSelected = true;
                                        }
                                    })
                                }
                            }
                            if (data["cmd"] == "featureUnselected") {
                                for (areaType in this.$root.masterOrderLine.allAvailableAreas[orderItem.metadata.uuid]) {
                                    this.$root.masterOrderLine.allAvailableAreas[orderItem.metadata.uuid][areaType].forEach(function (availableArea) {
                                        if (availableArea.code == areaname) {
                                            availableArea.isLocalSelected = false;
                                        }
                                    })
                                }
                            }
                            this.updateSelectedAreas();
                            this.updateAvailableProjections();
                            this.updateAvailableFormats();
                            this.$root.validateAreas();
                        }
                    }
                }
            }.bind(this));
        },
        loadPolygonMap: function (orderItem) {
            var coverageParams = "";
            $.ajax({
                url: '/api/getdata/' + orderItem.metadata.uuid,
                type: "GET",
                async: false,
                success: function (result) {
                    coverageParams = result.CoverageUrl;
                    if (typeof coverageParams == 'undefined') {
                        orderItem.mapData.coverageParams = coverageParams;
                    }
                }
            });
            orderItem.mapIsLoaded = true;
            orderItem.mapData.defaultConfigurations = {
                center_latitude: "7226208",
                center_longitude: "378604",
                grid_folder: "/sites/all/modules/custom/kms_widget/grid/",
                coordinateSystem: "32633",
                selection_type: "3525",
                service_name: "fylker-utm32",
                zoom_level: "4",
            }

            window.addEventListener('message', function (e) {
                if (e !== undefined && e.data !== undefined && typeof (e.data) == "string") {
                    var msg = JSON.parse(e.data);
                    if (msg.type === "mapInitialized") {
                        iframeMessage = {
                            "cmd": "setCenter",
                            "x": orderItem.mapData.defaultConfigurations.center_longitude,
                            "y": orderItem.mapData.defaultConfigurations.center_latitude,
                            "zoom": orderItem.mapData.defaultConfigurations.zoom_level
                        };
                        var iframeElement = document.getElementById(orderItem.metadata.uuid + "-iframe").contentWindow;
                        iframeElement.postMessage(JSON.stringify(iframeMessage), '*');
                    }
                    else if (msg.cmd === "setVisible") { return }
                    else {
                        var reslist = document.getElementById('result');
                        if (msg.feature != null) {

                            var coordinatesString = msg.feature.geometry.coordinates.toString();
                            coordinatesString = coordinatesString.replace(/,/g, " ");
                            var canDownloadData = {
                                "metadataUuid": orderItem.metadata.uuid,
                                "coordinates": coordinatesString,
                                "coordinateSystem": orderItem.mapData.defaultConfigurations.coordinateSystem
                            };
                            var urlCanDownload = (orderItem.metadata.canDownloadUrl !== undefined) ? orderItem.metadata.canDownloadUrl : false;
                            if (urlCanDownload) {
                                $.ajax({
                                    url: urlCanDownload,
                                    type: "POST",
                                    dataType: 'json',
                                    data: JSON.stringify(canDownloadData),
                                    contentType: "application/json",
                                    async: true,
                                    error: function (jqXHR, textStatus, errorThrown) {
                                        //Ignore error
                                    },
                                    beforeSend: function () {
                                        showLoadingAnimation("Sjekker størrelse for valgt område");
                                    },
                                    success: function (data) {
                                        if (data !== null) {
                                            if (!data.canDownload) {
                                                clearAlertMessage();
                                                showAlert("Området er for stort til å laste ned, vennligst velg mindre område", "danger");
                                            } else {
                                                clearAlertMessage();
                                                hideAlert();

                                                this.$root.masterOrderLine.allSelectedCoordinates[orderItem.metadata.uuid] = coordinatesString;
                                                var polygonArea = {
                                                    "name": "Valgt fra kart",
                                                    "type": "polygon",
                                                    "code": "Kart",
                                                    "isLocalSelected": true,
                                                    "formats": orderItem.defaultFormats,
                                                    "projections": orderItem.defaultProjections,
                                                    "coordinates": coordinatesString,
                                                    "coordinatesystem": orderItem.mapData.defaultConfigurations.coordinateSystem
                                                }

                                                polygonArea.allAvailableProjections = {};
                                                polygonArea.allAvailableProjections[orderItem.metadata.uuid] = orderItem.defaultProjections;

                                                polygonArea.allAvailableFormats = {};
                                                polygonArea.allAvailableFormats[orderItem.metadata.uuid] = orderItem.defaultFormats;

                                                var isAllreadyAddedInfo = this.isAllreadyAdded(this.$root.masterOrderLine.allSelectedAreas[orderItem.metadata.uuid], polygonArea, "code");
                                                if (!isAllreadyAddedInfo.added) {
                                                    this.$root.masterOrderLine.allSelectedAreas[orderItem.metadata.uuid].push(polygonArea);
                                                } else {
                                                    this.$root.masterOrderLine.allSelectedAreas[orderItem.metadata.uuid][isAllreadyAddedInfo.position] = polygonArea;

                                                }

                                                this.$root.masterOrderLine.allAvailableAreas[orderItem.metadata.uuid][polygonArea.type] = [];
                                                this.$root.masterOrderLine.allAvailableAreas[orderItem.metadata.uuid][polygonArea.type].push(polygonArea);


                                                // Set coordinates for orderline in order request
                                                this.$root.orderRequests[orderItem.metadata.orderDistributionUrl].orderLines.forEach(function (orderRequest) {
                                                    if (orderRequest.metadataUuid == orderItem.metadata.uuid) {
                                                        orderRequest.coordinates = this.$root.masterOrderLine.allSelectedCoordinates[orderItem.metadata.uuid];
                                                    }
                                                }.bind(this))

                                                this.$root.$forceUpdate();

                                                this.updateAvailableProjections();
                                                this.updateAvailableFormats();
                                                this.$root.validateAreas();
                                            }
                                        }
                                        hideLoadingAnimation();
                                    }.bind(this),
                                });
                            }

                        }
                    }
                }
            }.bind(this));

        },

    },
    components: {
        'areas': Areas,
        'projections': Projections,
        'formats': Formats
    },
};

var MasterOrderLine = {
    props: ['allAvailableAreas', 'allAvailableProjections', 'allAvailableFormats', 'allSelectedAreas', 'allSelectedProjections', 'allSelectedFormats', 'allOrderLineErrors'],
    data: function () {
        var data = {
            availableAreas: {},
            availableProjections: [],
            availableFormats: [],

            selectedAreas: [],
            selectedProjections: [],
            selectedFormats: []
        }
        return data;
    },
    created: function () {
        for (orderLine in this.allAvailableAreas) {

            if (this.$parent.masterOrderLine.allSelectedAreas[orderLine] == undefined) { this.$parent.masterOrderLine.allSelectedAreas[orderLine] = [] }
            if (this.$parent.masterOrderLine.allSelectedProjections[orderLine] == undefined) { this.$parent.masterOrderLine.allSelectedProjections[orderLine] = [] }
            if (this.$parent.masterOrderLine.allSelectedFormats[orderLine] == undefined) { this.$parent.masterOrderLine.allSelectedFormats[orderLine] = [] }

            for (areaType in this.allAvailableAreas[orderLine]) {
                this.allAvailableAreas[orderLine][areaType].forEach(function (area) {
                    if (this.availableAreas[areaType] == undefined) {
                        this.availableAreas[areaType] = [];
                    }

                    area.orderLineUuids = [];
                    area.orderLineUuids.push(orderLine);

                    if (area.allAvailableProjections == undefined) { area.allAvailableProjections = {} };
                    if (area.allAvailableProjections[orderLine] == undefined) { area.allAvailableProjections[orderLine] = [] };
                    area.allAvailableProjections[orderLine] = area.projections;

                    if (area.allAvailableFormats == undefined) { area.allAvailableFormats = {} };
                    if (area.allAvailableFormats[orderLine] == undefined) { area.allAvailableFormats[orderLine] = [] };
                    area.allAvailableFormats[orderLine] = area.formats;


                    var areaIsAllreadyAddedInfo = this.isAllreadyAdded(this.availableAreas[areaType], area, "code");


                    if (!areaIsAllreadyAddedInfo.added) {
                        this.availableAreas[areaType].push(area);
                    } else {
                        var orderLineUuidIsAdded = false

                        if (!orderLineUuidIsAdded) {

                            this.availableAreas[areaType][areaIsAllreadyAddedInfo.position].orderLineUuids.push(orderLine);

                            // Add available projections to area
                            if (this.availableAreas[areaType][areaIsAllreadyAddedInfo.position].allAvailableProjections[orderLine] == undefined) {
                                this.availableAreas[areaType][areaIsAllreadyAddedInfo.position].allAvailableProjections[orderLine] = [];
                            }
                            this.availableAreas[areaType][areaIsAllreadyAddedInfo.position].allAvailableProjections[orderLine] = area.projections;

                            // Add available formats to area
                            if (this.availableAreas[areaType][areaIsAllreadyAddedInfo.position].allAvailableFormats[orderLine] == undefined) {
                                this.availableAreas[areaType][areaIsAllreadyAddedInfo.position].allAvailableFormats[orderLine] = [];
                            }
                            this.availableAreas[areaType][areaIsAllreadyAddedInfo.position].allAvailableFormats[orderLine] = area.formats;

                        }
                    }
                    // }

                }.bind(this))
            }
        }
        this.$root.validateAreas();
        this.updateSelectedAreas();
    },
    methods: {
        isAllreadyAdded: function (array, item, propertyToCompare) {
            var isAllreadyAdded = {
                added: false,
                position: 0
            };
            array.forEach(function (arrayItem, index) {
                if (this.readProperty(arrayItem, propertyToCompare) == this.readProperty(item, propertyToCompare)) {
                    isAllreadyAdded.added = true
                    isAllreadyAdded.position = index;
                };
            }.bind(this))
            return isAllreadyAdded;
        },
        readProperty: function (obj, prop) {
            return obj[prop];
        },
        filterOptionList: function (optionListId, inputValue) {
            var dropdownListElements = document.getElementsByClassName(optionListId);
            var filter = inputValue.toUpperCase();
            for (var listIndex = 0; listIndex < dropdownListElements.length; listIndex++) {
                var listItems = dropdownListElements[listIndex].getElementsByTagName('li');
                var hasResults = false;
                for (var i = 0; i < listItems.length; i++) {
                    if (listItems[i].innerHTML.toUpperCase().indexOf(filter) > -1) {
                        listItems[i].style.display = "";
                        hasResults = true;
                    } else {
                        listItems[i].style.display = "none";
                    }
                }

                var optionGroupNameElement = $(dropdownListElements[listIndex]).closest("div").find(".custom-select-list-option-group-name");
                if (!hasResults) {
                    optionGroupNameElement.hide();
                } else {
                    optionGroupNameElement.show();
                }
            }
        },
        updateSelectedAreas: function () {
            var allSelectedAreas = {};
            var selectedAreas = [];
            for (areaType in this.availableAreas) {
                this.availableAreas[areaType].forEach(function (area) {
                    area.orderLineUuids.forEach(function (orderLineUuid) {
                        if (allSelectedAreas[orderLineUuid] == undefined) { allSelectedAreas[orderLineUuid] = [] }
                        if (area.isSelected || area.isLocalSelected) {
                            allSelectedAreas[orderLineUuid].push(area);
                        }
                    }.bind(this))

                    if (area.isSelected) {
                        var isAllreadyAddedInfo = this.isAllreadyAdded(selectedAreas, area, "code");
                        if (!isAllreadyAddedInfo.added) {
                            selectedAreas.push(area);
                        }
                    }

                    /* if (area.projections.length == 1) {
                         area.projections[0].isSelected = true;
                     }
                     if (area.formats.length == 1) {
                         area.formats[0].isSelected = true;
                     }*/

                }.bind(this));
            }
            this.$parent.masterOrderLine.allSelectedAreas = allSelectedAreas;
            this.selectedAreas = selectedAreas;

        },
        updateAvailableProjections: function () {
            var availableProjections = [];
            var allAvailableProjections = {}
            if (this.$parent.masterOrderLine.allSelectedAreas !== undefined) {
                for (orderLine in this.$parent.masterOrderLine.allSelectedAreas) {

                    this.$parent.masterOrderLine.allSelectedAreas[orderLine].forEach(function (selectedArea) {
                        if (selectedArea.allAvailableProjections !== undefined) {

                            // All available projections for orderLine
                            selectedArea.allAvailableProjections[orderLine].forEach(function (projection) {
                                if (projection.isSelected == undefined) { projection.isSelected = false }
                                if (projection.isLocalSelected == undefined) { projection.isLocalSelected = false }

                                // Update availableProjections array
                                var isAllreadyAddedInfo = this.isAllreadyAdded(availableProjections, projection, "code");
                                if (!isAllreadyAddedInfo.added) {
                                    availableProjections.push(projection);
                                }

                                // Update allAvailableProjections object
                                if (allAvailableProjections[orderLine] == undefined) { allAvailableProjections[orderLine] = [] }
                                var isAllreadyAddedInfo = this.isAllreadyAdded(allAvailableProjections[orderLine], projection, "code");
                                if (!isAllreadyAddedInfo.added) {
                                    allAvailableProjections[orderLine].push(projection);
                                }
                            }.bind(this))

                        }
                    }.bind(this))

                }
            }
            this.availableProjections = availableProjections;
            this.$parent.masterOrderLine.allAvailableProjections = allAvailableProjections;

        },
        updateAvailableFormats: function () {
            var availableFormats = [];
            var allAvailableFormats = {};
            if (this.$parent.masterOrderLine.allSelectedAreas !== undefined) {
                for (orderLine in this.$parent.masterOrderLine.allSelectedAreas) {

                    this.$parent.masterOrderLine.allSelectedAreas[orderLine].forEach(function (selectedArea) {
                        if (selectedArea.allAvailableFormats !== undefined) {

                            // All available formats for orderLine
                            selectedArea.allAvailableFormats[orderLine].forEach(function (format) {
                                if (format.isSelected == undefined) { format.isSelected = false }

                                // Update availableFormats array
                                var isAllreadyAddedInfo = this.isAllreadyAdded(availableFormats, format, "name");
                                if (!isAllreadyAddedInfo.added) {
                                    availableFormats.push(format);
                                }

                                // Update allAvailableFormats object
                                if (allAvailableFormats[orderLine] == undefined) { allAvailableFormats[orderLine] = [] }
                                var isAllreadyAddedInfo = this.isAllreadyAdded(allAvailableFormats[orderLine], format, "name");
                                if (!isAllreadyAddedInfo.added) {
                                    allAvailableFormats[orderLine].push(format);
                                }
                            }.bind(this))

                        }
                    }.bind(this))

                }
            }
            this.availableFormats = availableFormats;
            this.$parent.masterOrderLine.allAvailableFormats = allAvailableFormats;
        },

        updateSelectedProjections: function () {
            var allSelectedProjections = {};
            var selectedProjections = [];
            for (orderLine in this.$parent.masterOrderLine.allAvailableProjections) {
                this.$parent.masterOrderLine.allAvailableProjections[orderLine].forEach(function (availableProjection) {

                    if (availableProjection.isSelected || availableProjection.isLocalSelected) {

                        if (availableProjection.isSelected) {
                            // Update availableProjections array
                            var isAllreadyAddedInfo = this.isAllreadyAdded(selectedProjections, availableProjection, "code");
                            if (!isAllreadyAddedInfo.added) {
                                selectedProjections.push(availableProjection);
                            }
                        }

                        // Update allAvailableProjections object
                        if (allSelectedProjections[orderLine] == undefined) { allSelectedProjections[orderLine] = [] }
                        var isAllreadyAddedInfo = this.isAllreadyAdded(allSelectedProjections[orderLine], availableProjection, "code");
                        if (!isAllreadyAddedInfo.added) {
                            allSelectedProjections[orderLine].push(availableProjection);
                        }
                    }

                }.bind(this))
            }

            this.$parent.masterOrderLine.allSelectedProjections = allSelectedProjections;
            this.selectedProjections = selectedProjections;
        },

        updateSelectedFormats: function () {
            var allSelectedFormats = {};
            var selectedFormats = [];
            for (orderLine in this.$parent.masterOrderLine.allAvailableFormats) {
                this.$parent.masterOrderLine.allAvailableFormats[orderLine].forEach(function (availableFormats) {

                    if (availableFormats.isSelected || availableFormats.isLocalSelected) {

                        if (availableFormats.isSelected) {
                            // Update availableFormats array
                            var isAllreadyAddedInfo = this.isAllreadyAdded(selectedFormats, availableFormats, "name");
                            if (!isAllreadyAddedInfo.added) {
                                selectedFormats.push(availableFormats);
                            }
                        }

                        // Update allAvailableFormats object
                        if (allSelectedFormats[orderLine] == undefined) { allSelectedFormats[orderLine] = [] }
                        var isAllreadyAddedInfo = this.isAllreadyAdded(allSelectedFormats[orderLine], availableFormats, "name");
                        if (!isAllreadyAddedInfo.added) {
                            allSelectedFormats[orderLine].push(availableFormats);
                        }
                    }

                }.bind(this))
            }

            this.$parent.masterOrderLine.allSelectedFormats = allSelectedFormats;
            this.selectedFormats = selectedFormats;
        }
    },
    template: '#master-order-line-template',
    components: {
        'areas': Areas,
        'projections': Projections,
        'formats': Formats
    }
}

Vue.config.debug = true;

var mainVueModel = new Vue({
    el: '#vueContainer',
    data: {
        orderLines: [],
        email: "",
        orderResponse: {},
        emailRequired: false,

        masterOrderLine: {
            allAvailableAreas: {},
            allAvailableProjections: {},
            allAvailableFormats: {},
            allSelectedAreas: {},
            allSelectedProjections: {},
            allSelectedFormats: {},
            allOrderLineErrors: {},
            allSelectedCoordinates: {},
            allDefaultProjections: {},
            allDefaultFormats: {}
        }
    },
    computed: {

        orderRequests: function () {
            var orderRequests = {};
            if (this.orderLines.length) {
                this.orderLines.forEach(function (orderLine) {
                    if (orderRequests[orderLine.metadata.orderDistributionUrl] == undefined) {
                        orderRequests[orderLine.metadata.orderDistributionUrl] = {
                            "email": "",
                            "_links": "",
                            "orderLines": []
                        }
                    }

                    var links = [];
                    if (orderLine.capabilities._links !== undefined && orderLine.capabilities._links.length) {
                        orderLine.capabilities._links.forEach(function (capabilityLink) {
                            var link = {
                                "href": capabilityLink.href,
                                "rel": capabilityLink.rel,
                                "templated": capabilityLink.templatedSpecified,
                                "type": "",
                                "deprecation": "",
                                "name": "",
                                "title": ""
                            }
                            links.push(link);
                        })
                    }


                    var areas = [];
                    if (this.masterOrderLine.allSelectedAreas[orderLine.metadata.uuid] !== undefined && this.masterOrderLine.allSelectedAreas[orderLine.metadata.uuid].length) {
                        this.masterOrderLine.allSelectedAreas[orderLine.metadata.uuid].forEach(function (selectedArea) {
                            var area = {
                                "code": selectedArea.code,
                                "name": selectedArea.name,
                                "type": selectedArea.type,
                                "_links": []
                            }
                            areas.push(area);
                        });
                    }

                    var projections = [];
                    if (this.masterOrderLine.allSelectedProjections[orderLine.metadata.uuid] !== undefined && this.masterOrderLine.allSelectedProjections[orderLine.metadata.uuid].length) {
                        this.masterOrderLine.allSelectedProjections[orderLine.metadata.uuid].forEach(function (selectedProjection) {
                            var projection = {
                                "code": selectedProjection.code,
                                "name": selectedProjection.name,
                                "codespace": selectedProjection.codespace,
                                "_links": []
                            }
                            projections.push(projection);
                        });
                    }

                    var formats = [];
                    if (this.masterOrderLine.allSelectedFormats[orderLine.metadata.uuid] !== undefined && this.masterOrderLine.allSelectedFormats[orderLine.metadata.uuid].length) {
                        this.masterOrderLine.allSelectedFormats[orderLine.metadata.uuid].forEach(function (selectedFormat) {
                            var format = {
                                "code": "",
                                "name": selectedFormat.name,
                                "type": "",
                                "_links": []
                            }
                            formats.push(format);
                        });
                    }


                    var orderRequest = {
                        "metadataUuid": orderLine.metadata.uuid,
                        "areas": areas,
                        "projections": projections,
                        "formats": formats,
                        "_links": links
                    }

                    if (this.masterOrderLine.allSelectedCoordinates[orderLine.metadata.uuid] !== "") {
                        orderRequest.coordinates = this.masterOrderLine.allSelectedCoordinates[orderLine.metadata.uuid];
                    }

                    orderRequests[orderLine.metadata.orderDistributionUrl].orderLines.push(orderRequest);

                }.bind(this));
            }
            return orderRequests;
        }
    },
    created: function () {
        $("#vueContainer").removeClass("hidden");
        var defaultUrl = "https://nedlasting.geonorge.no/api/capabilities/";
        var orderItemsJson = (localStorage["orderItems"] != null) ? JSON.parse(localStorage["orderItems"]) : [];
        var orderLines = [];
        if (orderItemsJson != []) {
            $(orderItemsJson).each(function (key, val) {
                if (val !== undefined && val !== null && val !== "") {
                    var metadata = (localStorage[val + ".metadata"] !== undefined) ? JSON.parse(localStorage[val + ".metadata"]) : "";
                    var apiUrl = (metadata.distributionUrl !== undefined) ? metadata.distributionUrl : defaultUrl;

                    orderLines[key] = {
                        "metadata": metadata,
                        "capabilities": getJsonData(apiUrl + val),
                        "projectionAndFormatIsRequired": false
                    }

                    var uuid = metadata.uuid;

                    this.masterOrderLine.allAvailableProjections[uuid] = [];
                    this.masterOrderLine.allAvailableFormats[uuid] = [];
                    this.masterOrderLine.allSelectedCoordinates[uuid] = "";
                    this.masterOrderLine.allDefaultProjections[uuid] = [];
                    this.masterOrderLine.allDefaultFormats[uuid] = [];


                    if (orderLines[key].capabilities._links !== undefined && orderLines[key].capabilities._links.length) {
                        orderLines[key].capabilities._links.forEach(function (link) {
                            if (link.rel == "http://rel.geonorge.no/download/order") {
                                orderLines[key].metadata.orderDistributionUrl = link.href;
                            }
                            if (link.rel == "http://rel.geonorge.no/download/can-download") {
                                orderLines[key].metadata.canDownloadUrl = link.href;
                            }
                            if (link.rel == "http://rel.geonorge.no/download/area") {
                                var availableAreas = getJsonData(link.href);
                                this.masterOrderLine.allAvailableAreas[uuid] = {};

                                availableAreas.forEach(function (availableArea) {
                                    if (this.masterOrderLine.allAvailableAreas[uuid][availableArea.type] == undefined) {
                                        this.masterOrderLine.allAvailableAreas[uuid][availableArea.type] = [];
                                    }
                                    availableArea.isSelected = false;
                                    availableArea.isLocalSelected = false;
                                    this.masterOrderLine.allAvailableAreas[uuid][availableArea.type].push(availableArea);
                                }.bind(this))
                            }
                            if (link.rel == "http://rel.geonorge.no/download/projection") {
                                var defaultProjections = getJsonData(link.href)
                                orderLines[key].defaultProjections = defaultProjections;
                                this.masterOrderLine.allDefaultProjections[uuid] = defaultProjections;
                            }
                            if (link.rel == "http://rel.geonorge.no/download/format") {
                                var defaultFormats = getJsonData(link.href);
                                orderLines[key].defaultFormats = defaultFormats;
                                this.masterOrderLine.allDefaultFormats[uuid] = defaultFormats;
                            }
                        }.bind(this))
                    }

                    /*
                    if (link.rel == "http://rel.geonorge.no/download/can-download") {
                        orderItems[key].metadata.canDownloadUrl = link.href;
                    }
                    */

                    var distributionUrl = (orderLines[key].metadata.distributionUrl !== undefined) ? orderLines[key].metadata.distributionUrl : "";


                    orderLines[key].capabilities.supportsGridSelection = (orderLines[key].capabilities.mapSelectionLayer !== undefined && orderLines[key].capabilities.mapSelectionLayer !== "") ? true : false;
                }
            }.bind(this));
        }
        this.orderLines = orderLines;
    },
    components: {
        'orderLine': OrderLine,
        'masterOrderLine': MasterOrderLine
    },
    methods: {
        isAllreadyAdded: function (array, item, propertyToCompare) {
            var isAllreadyAdded = {
                added: false,
                position: 0
            };
            if (array.length) {
                array.forEach(function (arrayItem, index) {
                    if (this.readProperty(arrayItem, propertyToCompare) == this.readProperty(item, propertyToCompare)) {
                        isAllreadyAdded.added = true
                        isAllreadyAdded.position = index;
                    };
                }.bind(this))
            }
            return isAllreadyAdded;
        },
        readProperty: function (obj, prop) {
            return obj[prop];
        },
        isSupportedType: function (areaType) {
            var isSupportedType = false;
            var supportedAreaTypes = ["fylke", "kommune", "landsdekkende"];
            supportedAreaTypes.forEach(function (supportedAreaType) {
                if (areaType == supportedAreaType) isSupportedType = true;
            })
            return isSupportedType;
        },
        hasSelectedProjections: function (area, orderLine) {
            var hasSelectedProjections = false;

            if (area.allAvailableProjections !== undefined && area.allAvailableProjections[orderLine] !== undefined && area.allAvailableProjections[orderLine].length) {
                area.allAvailableProjections[orderLine].forEach(function (availableProjection) {
                    if (this.masterOrderLine.allSelectedProjections[orderLine] !== undefined && this.masterOrderLine.allSelectedProjections[orderLine].length) {
                        this.masterOrderLine.allSelectedProjections[orderLine].forEach(function (selectedProjection) {
                            if (selectedProjection.code == availableProjection.code) {
                                hasSelectedProjections = true
                            }
                        }.bind(this))
                    }
                }.bind(this))
            }
            if (!hasSelectedProjections) {
                var errorMessage = "Støttet projeksjon for " + area.name + " mangler";
                this.masterOrderLine.allOrderLineErrors[orderLine]["projection"].push(errorMessage);
            }
            return hasSelectedProjections;
        },
        hasSelectedFormats: function (area, orderLine) {
            var hasSelectedFormats = false;

            if (area.allAvailableFormats !== undefined && area.allAvailableFormats[orderLine] !== undefined && area.allAvailableFormats[orderLine].length) {
                area.allAvailableFormats[orderLine].forEach(function (availableFormat) {
                    if (this.masterOrderLine.allSelectedFormats[orderLine] !== undefined && this.masterOrderLine.allSelectedFormats[orderLine].length) {
                        this.masterOrderLine.allSelectedFormats[orderLine].forEach(function (selectedFormat) {
                            if (selectedFormat.name == availableFormat.name) {
                                hasSelectedFormats = true
                            }
                        }.bind(this))
                    }
                }.bind(this))
            }
            if (!hasSelectedFormats) {
                var errorMessage = "Støttet format for " + area.name + " mangler";
                this.masterOrderLine.allOrderLineErrors[orderLine]["format"].push(errorMessage);
            }
            return hasSelectedFormats;
        },
        validateAreas: function () {

            for (orderLine in this.masterOrderLine.allAvailableAreas) {
                this.masterOrderLine.allOrderLineErrors[orderLine] = {};
                this.masterOrderLine.allOrderLineErrors[orderLine]["projection"] = [];
                this.masterOrderLine.allOrderLineErrors[orderLine]["format"] = [];
                this.masterOrderLine.allOrderLineErrors[orderLine]["area"] = [];
                if (this.masterOrderLine.allSelectedAreas[orderLine] !== undefined && this.masterOrderLine.allSelectedAreas[orderLine].length) {

                    this.masterOrderLine.allSelectedAreas[orderLine].forEach(function (selectedArea) {
                        selectedArea.hasSelectedProjections = this.hasSelectedProjections(selectedArea, orderLine);
                        selectedArea.hasSelectedFormats = this.hasSelectedFormats(selectedArea, orderLine);
                    }.bind(this));

                } else {
                    this.masterOrderLine.allOrderLineErrors[orderLine]["area"] = ["Datasett mangler valgt område"];
                }
            }
            setTimeout(function () {
                $("[data-toggle='tooltip']").tooltip();
            }, 300);
        },

        updateAllOrderLineFields: function () {
            this.$children.forEach(function (orderLine, index) {
                orderLine.updateSelectedAreas();
                orderLine.updateAvailableProjections();
                orderLine.updateAvailableFormats();
            });
        },
        sendRequests: function () {
            var responseData = [];
            var responseFailed = false;
            var orderRequests = this.orderRequests;
            for (distributionUrl in orderRequests) {
                $.ajax({
                    url: distributionUrl,
                    type: "POST",
                    dataType: 'json',
                    data: JSON.stringify(orderRequests[distributionUrl]),
                    contentType: "application/json",
                    xhrFields: { withCredentials: IsGeonorge(distributionUrl) },
                    async: false,
                    error: function (jqXHR, textStatus, errorThrown) {
                        showAlert(errorThrown, "danger");
                        responseFailed = true;
                    },
                    success: function (data) {
                        if (data !== null) {
                            responseData.push(
                                {
                                    "distributionUrl": distributionUrl,
                                    "data": data
                                });
                        }
                        else {
                            showAlert("Feil", "danger");
                            responseFailed = true;
                        }
                    }
                }).done(function () {
                    $("[data-toggle='tooltip']").tooltip();
                })
            }

            if (!responseFailed) {
                mainVueModel.removeAllOrderItems();
            }
            this.orderResponse = responseData;
        },

        removeFromLocalStorage: function (uuid) {
            var uuidLength = uuid.length;
            var orderItems = JSON.parse(localStorage["orderItems"]);
            orderItems = $.grep(orderItems, function (value) {
                return value != uuid;
            });
            localStorage["orderItems"] = JSON.stringify(orderItems);
            Object.keys(localStorage)
                    .forEach(function (key) {
                        if (key.substring(0, uuidLength) == uuid) {
                            localStorage.removeItem(key);
                        }
                    });
            updateShoppingCart();
            updateShoppingCartCookie();
        },
        removeOrderItem: function (orderLine) {
            this.orderLines = this.orderLines.filter(function (obj) {
                return obj.metadata.uuid !== orderLine.metadata.uuid;
            });
            this.removeFromLocalStorage(orderLine.metadata.uuid);
        },
        removeAllOrderItems: function () {
            this.orderLines.forEach(function (orderLine) {
                this.removeOrderItem(orderLine);
            }.bind(this));
            $('#remove-all-items-modal').modal('hide');
        },

        orderItemHasCoordinates: function (orderItem) {
            return (orderItem.codelists.coordinates !== undefined) ? true : false;
        },
        orderHasCoordinates: function () {
            var hasCoordinates = false;
            this.orderLines.forEach(function (orderItem) {
                if (this.orderItemHasCoordinates(orderItem)) hasCoordinates = true;
            }.bind(this));
            return hasCoordinates;
        },
        emailAddressIsValid: function (email) {
            var regex = /^(([^<>()\[\]\\.,;:\s@"]+(\.[^<>()\[\]\\.,;:\s@"]+)*)|(".+"))@((\[[0-9]{1,3}\.[0-9]{1,3}\.[0-9]{1,3}\.[0-9]{1,3}])|(([a-zA-Z\-0-9]+\.)+[a-zA-Z]{2,}))$/;
            return regex.test(email);
        },
        isEmpty: function (item) {
            return (item == null || item == undefined || item.length == 0);
        },
        allRequiredProjectionAndFormatFieldsIsNotEmpty: function (orderLines) {
            var orderItemsIsValid = true;
            orderLines.forEach(function (orderItem) {
                if (orderItem.projectionAndFormatIsRequired) {
                    if (this.isEmpty(orderItem.codelists.selectedProjections) || this.isEmpty(orderItem.codelists.selectedFormats)) {
                        orderItemsIsValid = false;
                    }
                }
            }.bind(this));
            return orderItemsIsValid;
        },

        formIsValid: function () {
            var emailFieldNotEmpty = (this.email !== "") ? true : false;
            var emailAddressIsValid = this.emailAddressIsValid(this.email);
            var projectionAndFormatFieldsIsValid = this.allRequiredProjectionAndFormatFieldsIsNotEmpty(this.orderLines);
            var emailRequired = this.emailRequired;
            var formIsValid = ((emailFieldNotEmpty && emailRequired && emailAddressIsValid) || (!emailRequired)) ? true : false;
            return formIsValid;
        },
        projectionAndFormatIsRequired: function (orderItem) {
            var required = this.orderItemHasCoordinates(orderItem);
            return required;
        }
    }
});
