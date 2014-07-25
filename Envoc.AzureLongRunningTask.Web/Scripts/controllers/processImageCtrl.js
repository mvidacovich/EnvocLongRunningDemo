(function () {
    var longRunningApp = angular.module('longRunningApp', []);

    longRunningApp.controller('processImageCtrl', ['$scope', '$http', function ($scope, $http) {
        $scope.messages = [];
        var notifications = $.connection.notificationHub;
        notifications.client.process = function (message) {
            $scope.messages.push(message);
            $scope.$apply();
            console.log('got message');
            console.log(name);
        };
        $.connection.hub.start();

        var cleanUrl = '/DirectUpload/UploadFile';

        $scope.test = function () {
            $http.get('/Home/Test');
        };
        var settings = {
            runtimes: 'html5',
            url: cleanUrl,
            max_file_size: '2048mb',
            chunk_size: '4mb',
            container: 'uploadContainer',
            method: 'post',
            drop_element: 'dropArea',
            browse_button: 'dropArea',
            multipart: true, // We don't want anything other than the data in CORS
            headers: {
                'x-ms-blob-type': 'BlockBlob', // Required by azure
            },
            filters: [
                { title: "Image files", extensions: "gif,jpg,png,jpeg,pdf,tiff,tif,bmp" }
            ],

        };


        $scope.uploader = new plupload.Uploader(settings);
        $scope.uploader.init();


        $scope.uploader.bind('FilesAdded', function (up, files) {
            $scope.uploader.start();
        });
        $scope.uploader.bind('BeforeUpload', function (up, file) {
            // Give server the id for reference
            up.settings.url = cleanUrl + "?id=" + file.id;
        });
    }]);
})();