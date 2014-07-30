(function () {
    var longRunningApp = angular.module('longRunningApp', []);
    longRunningApp.directive('bsHolder', function () {
        return {
            link: function (scope, element, attrs) {
                Holder.run(element);
            }
        };
    });

    // ISSUE: this controller is doing everything! Don't do what I do - have services / factories for stuff
    longRunningApp.controller('processImageCtrl', ['$scope', '$http', function ($scope, $http) {

        var directUploadUrl = '/DirectUpload/UploadFile';
        var getUploadUrl = '/CorsUpload/UploadUrl';
        var completeUpload = '/CorsUpload/CompleteUpload';
        var runtime = 'html5'; //silverlight, flash, html5, html4

        // Custom state for uploads
        plupload.WAITING_FOR_URL = -1;
        plupload.WAITING_SEND_AZURE = 60;

        $scope.files = [];

        // ISSUE: In a perfect world this hub would be a service with PubSubJS
        var notifications = $.connection.notificationHub;
        notifications.client.process = function (message) {
            if (message && message.Type == "Completed") {
                var content = message.Content;
                var requestId = content.RequestId;
                var item = _.find($scope.files, function(x) {
                    return x.requestId == requestId;
                });

                // Set resulting URL for display
                if (item) {
                    item.resultUrl = content.ResultUrl;
                    $scope.$apply();
                }
            }
        };
        $.connection.hub.start();


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

        // start upload process
        var processFile = _.debounce(function (file) {
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
                url: getUploadUrl,
                params: {
                    id: file.id,
                    name: file.name,
                    size: file.size
                }
            }).success(function (data) {
                file.url = data;
                if (file.status == plupload.WAITING_FOR_URL) {
                    file.status = plupload.QUEUED;
                    // We have the CORS url, start uploading
                    $scope.uploader.start();
                }

                if (file.status == plupload.WAITING_SEND_AZURE) {
                    file.status = plupload.DONE;
                }
            }).error(function (data) {
                console.error(data);
            });
        }, 20, true);

        var settings = {
            runtimes: runtime,
            url: '/',
            max_file_size: '2048mb', // Limit in storage emulator
            chunk_size: '64kb',
            container: 'uploadContainer',
            method: 'put',
            drop_element: 'dropArea',
            browse_button: 'clickButton',
            multipart: false, // We don't want anything other than the data in CORS
            headers: {
                'x-ms-blob-type': 'BlockBlob', // Required by azure
            },
            filters: [
                { title: "Image files", extensions: "gif,jpg,png,jpeg,pdf,tiff,tif,bmp" }
            ],
            silverlight_xap_url: 'Scripts/Moxie.xap',
            flash_swf_url: 'Scripts/Moxie.swf',
        };
        if (runtime == "flash" || runtime == 'html4') {
            // Non-CORS runtimes :(
            settings.multipart = true;
            settings.headers = null;
            settings.method = 'POST';
            if (runtime == 'html4') {
                settings.max_file_size = '4mb'; // Poor limit for html4, since it has no chunking
            } else {
                settings.max_file_size = '400mb'; // Flash upload limit, chunking but loads file into memory
            }
        }
        $scope.uploader = new plupload.Uploader(settings);
        $scope.uploader.init();

        // Setting the details of the file for each one added
        $scope.uploader.bind('FilesAdded', function (up, files) {
            for (var i = 0, l = files.length; i < l; i++) {
                var file = files[i];
                file.attempts = 1;
                file.chunks = 0;
                file.url = null;
                file.status = plupload.WAITING_FOR_URL;
                processFile(file);
                $scope.files.push(file);
            }
        });

        $scope.uploader.bind('ChunkUploaded', function (up, file) {
            // Counting the number of blocks uploaded
            file.chunks++;
        });

        $scope.uploader.bind('Error', function (up, error) {
            if (error.file != null && (error.status == 403 || error.status == 0)) {
                error.file.status = plupload.WAITING_FOR_URL;
                processFile(file);
            }
        });

        $scope.uploader.bind('FileUploaded', function (up, file, info) {
            _.each(up.files, function (item) {
                if (item.status == plupload.WAITING_FOR_URL) {
                    processFile(item);
                }
            });

            if (runtime != "flash" && runtime != 'html4') {
                // We uploaded via CORS, we need to let our web server know it's done and ready to enqueue
                var chunks = [];

                // Adding all the chunks uploaded
                for (var i = 0; i < file.chunks; i++) {
                    chunks.push(getBlockIdFromInt(i));
                }

                // Send request
                $http({
                    method: 'post',
                    url: completeUpload,
                    data: {
                        uploadId: file.id,
                        chunks: chunks
                    }
                }).success(function (data) {
                    file.status = plupload.DONE;
                    file.requestId = data;
                }).error(function (data) {
                    console.error(data);
                });
            } else {
                file.status = plupload.DONE;
            }
        });

        $scope.uploader.bind('BeforeUpload', function (up, file) {
            // Give server the id for reference
            up.settings.url = file.url;
        });
    }]);
})();