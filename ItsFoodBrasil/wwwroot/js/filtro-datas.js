// Impede data inicial maior que a data final nos filtros.
// Uso: <form data-filtro-datas> com inputs marcados em data-data-inicial e data-data-final.
(function () {
    document.querySelectorAll('form[data-filtro-datas]').forEach(function (form) {
        var ini = form.querySelector('[data-data-inicial]');
        var fim = form.querySelector('[data-data-final]');
        if (!ini || !fim) return;

        function validar() {
            // o calendário já bloqueia os dias inválidos
            ini.max = fim.value || '';
            fim.min = ini.value || '';

            var invalido = ini.value && fim.value && ini.value > fim.value;
            ini.setCustomValidity(invalido ? 'A data inicial não pode ser maior que a data final.' : '');
            fim.setCustomValidity(invalido ? 'A data final não pode ser menor que a data inicial.' : '');
        }

        ini.addEventListener('change', validar);
        fim.addEventListener('change', validar);

        form.addEventListener('submit', function (e) {
            validar();
            if (!form.checkValidity()) {
                e.preventDefault();
                form.reportValidity();
            }
        });

        validar();
    });
})();
