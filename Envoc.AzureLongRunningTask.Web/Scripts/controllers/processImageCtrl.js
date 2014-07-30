(function () {
    var longRunningApp = angular.module('longRunningApp', []);

    // ISSUE: this controller is doing everything! Don't do what I do - have services / factories for stuff
    longRunningApp.controller('processImageCtrl', ['$scope', '$http', function ($scope, $http) {

        // Custom state for uploads
        plupload.WAITING_FOR_URL = -1;
        plupload.WAITING_SEND_AZURE = 60;

        $scope.files = [];
        // ISSUE: In a perfect world this hub would be a service with PubSubJS
        var notifications = $.connection.notificationHub;
        notifications.client.process = function (message) {
            $scope.$apply();
            console.log('got message');
            console.log(message);
        };
        $.connection.hub.start();

        var directUploadUrl = '/DirectUpload/UploadFile';

        var runtime = 'html5'; //silverlight, flash

        // Azure requires a base64 encoded block id to piece the file together
        var getBlockIdFromInt = function (n) {
            var width = 5;
            var padding = '0';
            n = n + '';
            n = n.length >= width ? n : new Array(width - n.length + 1).join(padding) + n;
            return btoa(n);
        };

        // This is injecting custom url paramters for use against Azure blob storage
        var oldBuilder = plupload.buildUrl;
        plupload.buildUrl = function (url, items) {
            // It is only applicable with >1 piece
            if (items.chunks > 1) {
                items.comp = 'block';
                items.blockid = getBlockIdFromInt(items.chunk);
            }
            return oldBuilder(url, items);
        };

        // This gets the URL to use
        var getUrl = _.debounce(function (file) {

            // Use the oldschool URL
            if (runtime == "flash" || runtime == 'html4') {
                file.url = directUploadUrl + "?id=" + file.id;
                file.status = plupload.QUEUED;
                $rootScope.uploader.start();
                return;
            }

            // Get the CORS Url
            $http({
                method: 'get',
                url: '/CorsUpload/UploadUrl',
                params: {
                    id: file.id,
                    name: file.name,
                    size: file.size
                }
            }).success(function (data) {
                file.url = data;
                console.log(data);
                if (file.status == plupload.WAITING_FOR_URL) {
                    file.status = plupload.QUEUED;
                    $scope.uploader.start();
                }

                if (file.status == plupload.WAITING_SEND_AZURE) {
                    file.status = plupload.DONE;
                }
                // We have the CORS url, start uploading
            }).error(function (data) {
                console.error(data);
                alert("something went terribly wrong");
            });
        }, 20, true);

        var settings = {
            runtimes: runtime,
            url: directUploadUrl,
            max_file_size: '2048mb', // Limit in storage emulator
            chunk_size: '4mb',
            container: 'uploadContainer',
            method: 'put',
            drop_element: 'dropArea',
            browse_button: 'dropArea',
            multipart: false, // We don't want anything other than the data in CORS
            headers: {
                'x-ms-blob-type': 'BlockBlob', // Required by azure
            },
            filters: [
                { title: "Image files", extensions: "gif,jpg,png,jpeg,pdf,tiff,tif,bmp" }
            ],
            silverlight_xap_url: 'Content/js/libs/Moxie.xap',
            flash_swf_url: 'Content/js/libs/Moxie.swf',
        };
        
        if (runtime == "flash" || runtime == 'html4') {
            // Non-CORS runtimes
            settings.multipart = true;
            settings.headers = null;
            settings.method = 'POST';
            if (runtime == 'html4') {
                settings.max_file_size = '4mb'; // Poor limit for html4, since it has no chunking
            } else {
                settings.max_file_size = '400mb'; // Flash upload limit
            }
        }

        $scope.uploader = new plupload.Uploader(settings);
        $scope.uploader.init();

        $scope.uploader.bind('FilesAdded', function (up, files) {
            for (var i = 0, l = files.length; i < l; i++) {
                var file = files[i];
                file.attempts = 1;
                file.chunks = 0;
                file.url = null;
                file.status = plupload.WAITING_FOR_URL;
                file.lastError = new Date();
                getUrl(file);
                $scope.files.push(file);
            }
        });

        $scope.uploader.bind('Error', function (up, error) {
            if (error.file != null) {
                error.file.status = plupload.WAITING_FOR_URL;
                getUrl(file);
            }
        });

        $scope.uploader.bind('BeforeUpload', function (up, file) {
            // Give server the id for reference
            console.log(file);
            up.settings.url = file.url + "?id=" + file.id;
        });
    }]);
})();