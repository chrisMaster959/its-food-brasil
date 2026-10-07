(function () {
    const $ = (id) => document.getElementById(id);
    const fmt = new Intl.NumberFormat('pt-BR', { style: 'currency', currency: 'BRL' });
    const money = (cents) => fmt.format(cents / 100);

    const catalogo = JSON.parse($('catalogo-json').textContent);
    let carrinho = JSON.parse($('carrinho-json').textContent); // [{ id, qtd }]
    const porId = {};
    catalogo.forEach((p) => { porId[p.id] = p; });

    const form = $('pdv-form');
    let sugestoes = [];
    let sel = -1;
    let avisoTimer;

    // ---------- utilidades ----------
    function esc(s) {
        const d = document.createElement('div');
        d.textContent = s;
        return d.innerHTML;
    }

    // "12,50" | "12.50" | "1.250,50" -> centavos (inteiro)
    function parseMoeda(str) {
        let s = (str || '').trim();
        if (!s) return 0;
        s = s.indexOf(',') >= 0 ? s.replace(/\./g, '').replace(',', '.') : s;
        const n = parseFloat(s);
        return isNaN(n) || n < 0 ? 0 : Math.round(n * 100);
    }

    function aviso(msg) {
        const el = $('aviso');
        el.textContent = msg;
        el.hidden = false;
        clearTimeout(avisoTimer);
        avisoTimer = setTimeout(() => { el.hidden = true; }, 3500);
    }

    // ---------- carrinho ----------
    function qtdNoCarrinho(id) {
        const c = carrinho.find((x) => x.id === id);
        return c ? c.qtd : 0;
    }

    function definirQtd(id, nova) {
        const p = porId[id];
        if (nova > p.estoque) {
            aviso('Estoque insuficiente para ' + p.nome + ' (disponível: ' + p.estoque + ').');
            return false;
        }
        const c = carrinho.find((x) => x.id === id);
        if (nova <= 0) {
            carrinho = carrinho.filter((x) => x.id !== id);
        } else if (c) {
            c.qtd = nova;
        } else {
            carrinho.push({ id: id, qtd: nova });
        }
        render();
        return true;
    }

    function adicionar(id, qtd) {
        if (definirQtd(id, qtdNoCarrinho(id) + qtd)) {
            const box = $('cart-box');
            box.scrollTop = box.scrollHeight;
            return true;
        }
        return false;
    }

    // ---------- totais e tela ----------
    function totais() {
        let subtotal = 0, itens = 0;
        carrinho.forEach((c) => {
            subtotal += Math.round(porId[c.id].preco * 100) * c.qtd;
            itens += c.qtd;
        });
        const desconto = Math.min(parseMoeda($('desconto').value), subtotal);
        const total = subtotal - desconto;
        const recebido = parseMoeda($('recebido').value);
        const dinheiro = $('forma').value === 'Dinheiro';
        const troco = dinheiro ? Math.max(0, recebido - total) : 0;
        return { subtotal, itens, desconto, total, recebido, troco, dinheiro };
    }

    function linha(c, i) {
        const p = porId[c.id];
        const sub = Math.round(p.preco * 100) * c.qtd;
        return '<tr>' +
            '<td>' + (i + 1) + '</td>' +
            '<td><strong>' + esc(p.nome) + '</strong><div class="muted">Cód. ' + p.id + '</div></td>' +
            '<td><div class="qty">' +
                '<button type="button" class="qbtn" data-acao="menos" data-id="' + p.id + '">−</button>' +
                '<span>' + c.qtd + '</span>' +
                '<button type="button" class="qbtn" data-acao="mais" data-id="' + p.id + '">+</button>' +
            '</div></td>' +
            '<td>' + fmt.format(p.preco) + '</td>' +
            '<td><strong>' + money(sub) + '</strong></td>' +
            '<td class="actions"><button type="button" class="icon-btn danger" data-acao="remover" data-id="' + p.id + '" title="Remover"><i class="bi bi-trash"></i></button></td>' +
            '</tr>';
    }

    function render() {
        $('itens').innerHTML = carrinho.map(linha).join('');
        $('tabela').hidden = carrinho.length === 0;
        $('vazio').hidden = carrinho.length > 0;

        const t = totais();
        $('sum-itens').textContent = t.itens;
        $('sum-subtotal').textContent = money(t.subtotal);
        $('sum-total').textContent = money(t.total);
        $('sum-troco').textContent = money(t.troco);
        $('box-dinheiro').hidden = !t.dinheiro;
        $('btn-finalizar').disabled = carrinho.length === 0;
    }

    $('itens').addEventListener('click', (e) => {
        const btn = e.target.closest('button[data-acao]');
        if (!btn) return;
        const id = parseInt(btn.dataset.id, 10);
        const acao = btn.dataset.acao;
        if (acao === 'mais') definirQtd(id, qtdNoCarrinho(id) + 1);
        else if (acao === 'menos') definirQtd(id, qtdNoCarrinho(id) - 1);
        else definirQtd(id, 0);
    });

    // ---------- busca ----------
    function buscar(termo) {
        const t = termo.trim().toLowerCase();
        if (!t) return [];
        return catalogo.filter((p) => String(p.id) === t || p.nome.toLowerCase().indexOf(t) >= 0).slice(0, 6);
    }

    function fecharSugestoes() {
        $('sugestoes').hidden = true;
        sugestoes = [];
        sel = -1;
    }

    function mostrarSugestoes() {
        sugestoes = buscar($('busca').value);
        sel = -1;
        const box = $('sugestoes');
        if (!sugestoes.length) { box.hidden = true; return; }
        box.innerHTML = sugestoes.map((p, i) =>
            '<button type="button" class="sug" data-i="' + i + '">' +
            '<span>' + esc(p.nome) + '<small>Cód. ' + p.id + ' · estoque ' + p.estoque + '</small></span>' +
            '<strong>' + fmt.format(p.preco) + '</strong></button>').join('');
        box.hidden = false;
    }

    function realcar() {
        document.querySelectorAll('#sugestoes .sug').forEach((el, i) => {
            el.classList.toggle('on', i === sel);
        });
    }

    function qtdDigitada() {
        const q = parseInt($('qtd').value, 10);
        return isNaN(q) || q < 1 ? 1 : q;
    }

    function escolher(produto) {
        if (adicionar(produto.id, qtdDigitada())) {
            $('busca').value = '';
            $('qtd').value = 1;
            fecharSugestoes();
            $('busca').focus();
        }
    }

    function confirmarBusca() {
        if (sel >= 0 && sugestoes[sel]) return escolher(sugestoes[sel]);
        const lista = sugestoes.length ? sugestoes : buscar($('busca').value);
        if (!lista.length) { aviso('Produto não encontrado.'); return; }
        const exato = lista.find((p) => String(p.id) === $('busca').value.trim());
        escolher(exato || lista[0]);
    }

    $('busca').addEventListener('input', mostrarSugestoes);
    $('busca').addEventListener('keydown', (e) => {
        if (e.key === 'ArrowDown' && sugestoes.length) {
            e.preventDefault(); sel = (sel + 1) % sugestoes.length; realcar();
        } else if (e.key === 'ArrowUp' && sugestoes.length) {
            e.preventDefault(); sel = (sel - 1 + sugestoes.length) % sugestoes.length; realcar();
        } else if (e.key === 'Enter') {
            e.preventDefault(); confirmarBusca();
        } else if (e.key === 'Escape') {
            fecharSugestoes();
        }
    });
    $('qtd').addEventListener('keydown', (e) => {
        if (e.key === 'Enter') { e.preventDefault(); confirmarBusca(); }
    });
    $('btn-add').addEventListener('click', confirmarBusca);
    $('sugestoes').addEventListener('mousedown', (e) => {
        const b = e.target.closest('.sug');
        if (!b) return;
        e.preventDefault();
        escolher(sugestoes[parseInt(b.dataset.i, 10)]);
    });
    $('busca').addEventListener('blur', () => setTimeout(fecharSugestoes, 100));

    // ---------- resumo ----------
    ['desconto', 'recebido'].forEach((id) => $(id).addEventListener('input', render));
    $('forma').addEventListener('change', render);

    // Enter nos campos de texto não deve enviar o formulário sem querer
    form.addEventListener('keydown', (e) => {
        if (e.key === 'Enter' && e.target.tagName === 'INPUT') e.preventDefault();
    });

    // ---------- finalizar / cancelar ----------
    form.addEventListener('submit', (e) => {
        const t = totais();
        if (!carrinho.length) { e.preventDefault(); aviso('Adicione ao menos um produto.'); return; }
        if (t.dinheiro && t.recebido < t.total) { e.preventDefault(); aviso('O valor recebido é menor que o total.'); return; }

        const h = $('hidden-fields');
        h.innerHTML = '';
        const add = (name, value) => {
            const i = document.createElement('input');
            i.type = 'hidden'; i.name = name; i.value = value;
            h.appendChild(i);
        };
        carrinho.forEach((c, i) => {
            add('Itens[' + i + '].IdProduto', c.id);
            add('Itens[' + i + '].Quantidade', c.qtd);
        });
        add('DescontoCentavos', t.desconto);
        if (t.dinheiro) add('ValorRecebidoCentavos', t.recebido);
        $('btn-finalizar').disabled = true; // evita clique duplo
    });

    function cancelar() {
        if (!carrinho.length) return;
        if (!confirm('Cancelar a venda atual?')) return;
        carrinho = [];
        $('desconto').value = '0,00';
        $('recebido').value = '';
        render();
        $('busca').focus();
    }
    $('btn-cancelar').addEventListener('click', cancelar);

    // ---------- atalhos ----------
    document.addEventListener('keydown', (e) => {
        if (e.key === 'F2') { e.preventDefault(); $('busca').focus(); }
        else if (e.key === 'F8') { e.preventDefault(); cancelar(); }
        else if (e.key === 'F9') {
            e.preventDefault();
            if (form.requestSubmit) form.requestSubmit(); else $('btn-finalizar').click();
        }
    });

    // ---------- estado inicial (se a página voltou com erro) ----------
    const d = parseInt(form.dataset.desconto, 10);
    if (d > 0) $('desconto').value = (d / 100).toFixed(2).replace('.', ',');
    const r = parseInt(form.dataset.recebido, 10);
    if (r > 0) $('recebido').value = (r / 100).toFixed(2).replace('.', ',');
    if (form.dataset.forma) $('forma').value = form.dataset.forma;

    render();
    $('busca').focus();
})();