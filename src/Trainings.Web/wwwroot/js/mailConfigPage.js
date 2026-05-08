window.trainings = window.trainings || {};
window.trainings.mailConfig = window.trainings.mailConfig || {
    getClientLocalDateTime: function () {
        return new Date().toLocaleString();
    }
};
