/* =====================================================================
   ItsFoodBrasil - Distribuição de Embutidos
   Script completo para SQL Server (2016 SP1 ou superior)

   Tabelas: Pessoa, Funcionario, Categoria, Produto,
            Venda, ItemVenda, Movimentacao.

   Pessoa = IdPessoa, Nome, Email, Senha (sem CPF, Telefone e
   DataNascimento).

   Esta versão já inclui os ajustes da integração com o .NET
   (Senha VARCHAR(255) para guardar hash, Movimentacao.Categoria,
   vw_FluxoCaixa com Categoria, sp_RegistrarMovimentacao com
   @Categoria). Não precisa mais rodar o script de ajustes.

   Ordem do script:
     1. Banco de dados
     2. Tabelas, constraints e índices
     3. Tipo de tabela (TVP) usado na venda
     4. Functions
     5. Views
     6. Procedures
     7. Dados de exemplo (só se o banco estiver vazio)

   O script pode ser reexecutado; objetos e dados de exemplo são protegidos contra duplicação quando possível.
   Salve/abra como UTF-8 para os acentos ('Concluída', 'Saída') ficarem certos.
   ===================================================================== */

IF DB_ID(N'ItsFoodBrasil') IS NULL
    CREATE DATABASE ItsFoodBrasil COLLATE Latin1_General_100_CI_AI;
GO

USE ItsFoodBrasil;
GO

SET NOCOUNT ON;
GO


/* =====================================================================
   2. TABELAS
   ===================================================================== */

-- ---------------------------------------------------------------------
-- Pessoa
-- Senha guarda o HASH da senha, por isso VARCHAR(255).
-- ---------------------------------------------------------------------
IF OBJECT_ID(N'dbo.Pessoa', N'U') IS NULL
CREATE TABLE dbo.Pessoa
(
    IdPessoa  INT IDENTITY(1,1) NOT NULL,
    Nome      VARCHAR(150)      NOT NULL,
    Email     VARCHAR(150)      NULL,
    Senha     VARCHAR(255)      NULL,

    CONSTRAINT PK_Pessoa PRIMARY KEY (IdPessoa),
    CONSTRAINT CK_Pessoa_Nome CHECK (LEN(LTRIM(RTRIM(Nome))) > 0)
);
GO

-- Banco já existente: garante o tamanho da senha (hash)
IF COL_LENGTH(N'dbo.Pessoa', N'Senha') IS NOT NULL
   AND COL_LENGTH(N'dbo.Pessoa', N'Senha') < 255
    ALTER TABLE dbo.Pessoa ALTER COLUMN Senha VARCHAR(255) NULL;
GO

-- E-mail é único quando informado
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UX_Pessoa_Email' AND object_id = OBJECT_ID(N'dbo.Pessoa'))
    CREATE UNIQUE INDEX UX_Pessoa_Email ON dbo.Pessoa (Email) WHERE Email IS NOT NULL;
GO

-- ---------------------------------------------------------------------
-- Funcionario (herda de Pessoa: mesma chave)
-- ---------------------------------------------------------------------
IF OBJECT_ID(N'dbo.Funcionario', N'U') IS NULL
CREATE TABLE dbo.Funcionario
(
    IdPessoa  INT         NOT NULL,
    Perfil    VARCHAR(20) NOT NULL,
    Ativo     BIT         NOT NULL CONSTRAINT DF_Funcionario_Ativo DEFAULT (1),

    CONSTRAINT PK_Funcionario PRIMARY KEY (IdPessoa),
    CONSTRAINT FK_Funcionario_Pessoa FOREIGN KEY (IdPessoa) REFERENCES dbo.Pessoa (IdPessoa),
    CONSTRAINT CK_Funcionario_Perfil CHECK (Perfil IN ('Admin', 'Funcionario'))
);
GO

-- ---------------------------------------------------------------------
-- Categoria
-- ---------------------------------------------------------------------
IF OBJECT_ID(N'dbo.Categoria', N'U') IS NULL
CREATE TABLE dbo.Categoria
(
    IdCategoria  INT IDENTITY(1,1) NOT NULL,
    Nome         VARCHAR(80)       NOT NULL,
    Descricao    VARCHAR(250)      NULL,
    Ativa        BIT               NOT NULL CONSTRAINT DF_Categoria_Ativa DEFAULT (1),

    CONSTRAINT PK_Categoria PRIMARY KEY (IdCategoria),
    CONSTRAINT UQ_Categoria_Nome UNIQUE (Nome),
    CONSTRAINT CK_Categoria_Nome CHECK (LEN(LTRIM(RTRIM(Nome))) > 0)
);
GO

-- ---------------------------------------------------------------------
-- Produto
-- ---------------------------------------------------------------------
IF OBJECT_ID(N'dbo.Produto', N'U') IS NULL
CREATE TABLE dbo.Produto
(
    IdProduto          INT IDENTITY(1,1) NOT NULL,
    IdCategoria        INT               NOT NULL,
    Nome               VARCHAR(150)      NOT NULL,
    Descricao          VARCHAR(500)      NULL,
    Preco              DECIMAL(18,2)     NOT NULL,
    QuantidadeEstoque  INT               NOT NULL CONSTRAINT DF_Produto_Qtd DEFAULT (0),
    EstoqueMinimo      INT               NOT NULL CONSTRAINT DF_Produto_Min DEFAULT (20),
    DataValidade       DATE              NULL,
    Ativo              BIT               NOT NULL CONSTRAINT DF_Produto_Ativo DEFAULT (1),

    CONSTRAINT PK_Produto PRIMARY KEY (IdProduto),
    CONSTRAINT FK_Produto_Categoria FOREIGN KEY (IdCategoria) REFERENCES dbo.Categoria (IdCategoria),
    CONSTRAINT CK_Produto_Nome   CHECK (LEN(LTRIM(RTRIM(Nome))) > 0),
    CONSTRAINT CK_Produto_Preco  CHECK (Preco >= 0),
    CONSTRAINT CK_Produto_Qtd    CHECK (QuantidadeEstoque >= 0),
    CONSTRAINT CK_Produto_Min    CHECK (EstoqueMinimo >= 0)
);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Produto_Categoria' AND object_id = OBJECT_ID(N'dbo.Produto'))
    CREATE INDEX IX_Produto_Categoria ON dbo.Produto (IdCategoria);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Produto_Nome' AND object_id = OBJECT_ID(N'dbo.Produto'))
    CREATE INDEX IX_Produto_Nome ON dbo.Produto (Nome);
GO

-- ---------------------------------------------------------------------
-- Venda
-- ValorTotal já considera o desconto (igual à tela de vendas).
-- ---------------------------------------------------------------------
IF OBJECT_ID(N'dbo.Venda', N'U') IS NULL
CREATE TABLE dbo.Venda
(
    IdVenda        INT IDENTITY(1001,1) NOT NULL,
    IdFuncionario  INT                  NOT NULL,
    DataVenda      DATETIME             NOT NULL CONSTRAINT DF_Venda_Data DEFAULT (GETDATE()),
    ValorTotal     DECIMAL(18,2)        NOT NULL CONSTRAINT DF_Venda_Total DEFAULT (0),
    Desconto       DECIMAL(18,2)        NOT NULL CONSTRAINT DF_Venda_Desc DEFAULT (0),
    Status         VARCHAR(15)          NOT NULL CONSTRAINT DF_Venda_Status DEFAULT ('Concluída'),

    CONSTRAINT PK_Venda PRIMARY KEY (IdVenda),
    CONSTRAINT FK_Venda_Funcionario FOREIGN KEY (IdFuncionario) REFERENCES dbo.Funcionario (IdPessoa),
    CONSTRAINT CK_Venda_Valores CHECK (ValorTotal >= 0 AND Desconto >= 0),
    CONSTRAINT CK_Venda_Status  CHECK (Status IN ('Concluída', 'Pendente', 'Cancelada'))
);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Venda_Data' AND object_id = OBJECT_ID(N'dbo.Venda'))
    CREATE INDEX IX_Venda_Data ON dbo.Venda (DataVenda DESC) INCLUDE (Status, ValorTotal);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Venda_Funcionario' AND object_id = OBJECT_ID(N'dbo.Venda'))
    CREATE INDEX IX_Venda_Funcionario ON dbo.Venda (IdFuncionario);
GO

-- ---------------------------------------------------------------------
-- ItemVenda (PK composta: venda + produto)
-- ---------------------------------------------------------------------
IF OBJECT_ID(N'dbo.ItemVenda', N'U') IS NULL
CREATE TABLE dbo.ItemVenda
(
    IdVenda        INT           NOT NULL,
    IdProduto      INT           NOT NULL,
    Quantidade     INT           NOT NULL,
    PrecoUnitario  DECIMAL(18,2) NOT NULL,
    Subtotal       AS (CONVERT(DECIMAL(18,2), Quantidade * PrecoUnitario)) PERSISTED,

    CONSTRAINT PK_ItemVenda PRIMARY KEY (IdVenda, IdProduto),
    CONSTRAINT FK_ItemVenda_Venda   FOREIGN KEY (IdVenda)   REFERENCES dbo.Venda (IdVenda),
    CONSTRAINT FK_ItemVenda_Produto FOREIGN KEY (IdProduto) REFERENCES dbo.Produto (IdProduto),
    CONSTRAINT CK_ItemVenda_Qtd   CHECK (Quantidade > 0),
    CONSTRAINT CK_ItemVenda_Preco CHECK (PrecoUnitario >= 0)
);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_ItemVenda_Produto' AND object_id = OBJECT_ID(N'dbo.ItemVenda'))
    CREATE INDEX IX_ItemVenda_Produto ON dbo.ItemVenda (IdProduto);
GO

-- ---------------------------------------------------------------------
-- Movimentacao (fluxo de caixa)
-- IdFuncionario: opcional (quem lançou). IdVenda: opcional (entrada gerada por venda).
-- Categoria: texto livre do lançamento (vendas ficam sem categoria; a view mostra 'Vendas').
-- Cada venda gera no máximo uma movimentação (índice único filtrado).
-- ---------------------------------------------------------------------
IF OBJECT_ID(N'dbo.Movimentacao', N'U') IS NULL
CREATE TABLE dbo.Movimentacao
(
    IdMovimentacao    INT IDENTITY(1,1) NOT NULL,
    Tipo              VARCHAR(10)       NOT NULL,
    Valor             DECIMAL(18,2)     NOT NULL,
    Descricao         VARCHAR(200)      NOT NULL,
    Categoria         VARCHAR(50)       NULL,
    DataMovimentacao  DATETIME          NOT NULL CONSTRAINT DF_Mov_Data DEFAULT (GETDATE()),
    IdFuncionario     INT               NULL,
    IdVenda           INT               NULL,

    CONSTRAINT PK_Movimentacao PRIMARY KEY (IdMovimentacao),
    CONSTRAINT FK_Mov_Funcionario FOREIGN KEY (IdFuncionario) REFERENCES dbo.Funcionario (IdPessoa),
    CONSTRAINT FK_Mov_Venda       FOREIGN KEY (IdVenda)       REFERENCES dbo.Venda (IdVenda),
    CONSTRAINT CK_Mov_Tipo  CHECK (Tipo IN ('Entrada', 'Saída')),
    CONSTRAINT CK_Mov_Valor CHECK (Valor > 0)
);
GO

-- Banco já existente: garante a coluna Categoria
IF COL_LENGTH(N'dbo.Movimentacao', N'Categoria') IS NULL
    ALTER TABLE dbo.Movimentacao ADD Categoria VARCHAR(50) NULL;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UX_Mov_Venda' AND object_id = OBJECT_ID(N'dbo.Movimentacao'))
    CREATE UNIQUE INDEX UX_Mov_Venda ON dbo.Movimentacao (IdVenda) WHERE IdVenda IS NOT NULL;
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Mov_Data' AND object_id = OBJECT_ID(N'dbo.Movimentacao'))
    CREATE INDEX IX_Mov_Data ON dbo.Movimentacao (DataMovimentacao) INCLUDE (Tipo, Valor);
GO


/* =====================================================================
   3. TIPO DE TABELA (itens da venda enviados para a procedure)
   ===================================================================== */

IF TYPE_ID(N'dbo.TipoItemVenda') IS NULL
CREATE TYPE dbo.TipoItemVenda AS TABLE
(
    IdProduto   INT NOT NULL,
    Quantidade  INT NOT NULL
);
GO






/* =====================================================================
   4. FUNCTIONS
   ===================================================================== */

-- Pessoa não tem mais CPF: a função de validação deixou de ser usada.
IF OBJECT_ID(N'dbo.fn_CpfValido', N'FN') IS NOT NULL
    DROP FUNCTION dbo.fn_CpfValido;
GO

-- Saldo do caixa antes da primeira movimentação (ajuste conforme a empresa).
CREATE OR ALTER FUNCTION dbo.fn_SaldoInicialCaixa()
RETURNS DECIMAL(18,2)
AS
BEGIN
    RETURN 12450.00;
END;
GO

-- Saldo do caixa ao final do dia informado.
CREATE OR ALTER FUNCTION dbo.fn_SaldoCaixaAte(@Data DATE)
RETURNS DECIMAL(18,2)
AS
BEGIN
    DECLARE @Liquido DECIMAL(18,2);

    SELECT @Liquido = ISNULL(SUM(CASE WHEN Tipo = 'Entrada' THEN Valor ELSE -Valor END), 0)
    FROM dbo.Movimentacao
    WHERE DataMovimentacao < DATEADD(DAY, 1, CAST(@Data AS DATETIME));

    RETURN dbo.fn_SaldoInicialCaixa() + @Liquido;
END;
GO

-- Total de uma venda recalculado pelos itens (subtotal - desconto).
CREATE OR ALTER FUNCTION dbo.fn_TotalVenda(@IdVenda INT)
RETURNS DECIMAL(18,2)
AS
BEGIN
    DECLARE @Total DECIMAL(18,2);

    SELECT @Total = ISNULL((SELECT SUM(Subtotal) FROM dbo.ItemVenda WHERE IdVenda = @IdVenda), 0)
                    - ISNULL((SELECT Desconto FROM dbo.Venda WHERE IdVenda = @IdVenda), 0);

    RETURN @Total;
END;
GO

-- Itens de uma venda (tela de detalhes).
CREATE OR ALTER FUNCTION dbo.fn_ItensDaVenda(@IdVenda INT)
RETURNS TABLE
AS
RETURN
(
    SELECT  i.IdVenda,
            i.IdProduto,
            p.Nome AS Produto,
            i.Quantidade,
            i.PrecoUnitario,
            i.Subtotal
    FROM dbo.ItemVenda i
    JOIN dbo.Produto  p ON p.IdProduto = i.IdProduto
    WHERE i.IdVenda = @IdVenda
);
GO

-- Vendas de um período (datas inclusivas).
CREATE OR ALTER FUNCTION dbo.fn_VendasPorPeriodo(@DataInicial DATE, @DataFinal DATE)
RETURNS TABLE
AS
RETURN
(
    SELECT  v.IdVenda, v.IdFuncionario, v.DataVenda, v.ValorTotal, v.Desconto, v.Status
    FROM dbo.Venda v
    WHERE v.DataVenda >= CAST(@DataInicial AS DATETIME)
      AND v.DataVenda <  DATEADD(DAY, 1, CAST(@DataFinal AS DATETIME))
);
GO

-- Movimentações de um período (datas inclusivas).
CREATE OR ALTER FUNCTION dbo.fn_MovimentacoesPorPeriodo(@DataInicial DATE, @DataFinal DATE)
RETURNS TABLE
AS
RETURN
(
    SELECT  m.IdMovimentacao, m.DataMovimentacao, m.Descricao, m.Tipo, m.Valor, m.IdFuncionario, m.IdVenda
    FROM dbo.Movimentacao m
    WHERE m.DataMovimentacao >= CAST(@DataInicial AS DATETIME)
      AND m.DataMovimentacao <  DATEADD(DAY, 1, CAST(@DataFinal AS DATETIME))
);
GO


/* =====================================================================
   5. VIEWS
   ===================================================================== */

-- 1 -> Funcionários com os dados da pessoa (tela de Cadastros).
CREATE OR ALTER VIEW dbo.vw_Funcionarios
AS
SELECT  f.IdPessoa AS IdFuncionario,
        p.Nome,
        p.Email,
        f.Perfil,
        f.Ativo
FROM dbo.Funcionario f
JOIN dbo.Pessoa      p ON p.IdPessoa = f.IdPessoa;
GO
-- Teste
SELECT * FROM vw_Funcionarios
GO
-- 2 -> Estoque com os alertas (sem estoque, baixo, vencido, validade próxima).
CREATE OR ALTER VIEW dbo.vw_Estoque
AS
SELECT  p.IdProduto,
        p.IdCategoria,
        c.Nome AS Categoria,
        p.Nome,
        p.Descricao,
        p.Preco,
        p.QuantidadeEstoque AS Estoque,
        p.EstoqueMinimo,
        p.DataValidade,
        p.Ativo,
        d.DiasParaVencer,
        CAST(CASE WHEN p.QuantidadeEstoque <= 0 THEN 1 ELSE 0 END AS BIT) AS SemEstoque,
        CAST(CASE WHEN p.QuantidadeEstoque > 0 AND p.QuantidadeEstoque < p.EstoqueMinimo THEN 1 ELSE 0 END AS BIT) AS EstoqueBaixo,
        CAST(CASE WHEN d.DiasParaVencer < 0 THEN 1 ELSE 0 END AS BIT) AS Vencido,
        CAST(CASE WHEN d.DiasParaVencer BETWEEN 0 AND 15 THEN 1 ELSE 0 END AS BIT) AS ValidadeProxima,
        CAST(CASE WHEN p.Ativo = 1 AND
                       (p.QuantidadeEstoque <= 0
                        OR p.QuantidadeEstoque < p.EstoqueMinimo
                        OR d.DiasParaVencer <= 15)
                  THEN 1 ELSE 0 END AS BIT) AS EmAlerta,
        CONVERT(DECIMAL(18,2), p.Preco * p.QuantidadeEstoque) AS ValorEmEstoque
FROM dbo.Produto   p
JOIN dbo.Categoria c ON c.IdCategoria = p.IdCategoria
CROSS APPLY (SELECT DATEDIFF(DAY, CAST(GETDATE() AS DATE), p.DataValidade) AS DiasParaVencer) d;
GO
--Teste
SELECT * FROM vw_Estoque
GO

-- 3 -> Lista de vendas (tela Vendas, infos mais detalhadas).
CREATE OR ALTER VIEW dbo.vw_Vendas
AS
SELECT  v.IdVenda,
        v.DataVenda,
        v.IdFuncionario,
        pe.Nome AS Funcionario,
        ISNULL(i.QtdItens, 0)  AS QtdItens,
        ISNULL(i.Subtotal, 0)  AS Subtotal,
        v.Desconto,
        v.ValorTotal,
        v.Status
FROM dbo.Venda  v
JOIN dbo.Pessoa pe ON pe.IdPessoa = v.IdFuncionario
OUTER APPLY (SELECT SUM(Quantidade) AS QtdItens, SUM(Subtotal) AS Subtotal
             FROM dbo.ItemVenda WHERE IdVenda = v.IdVenda) i;
GO

--Teste
SELECT * FROM vw_Vendas
GO

-- 4 -> Itens vendidos (tela de detalhes da venda).
CREATE OR ALTER VIEW dbo.vw_ItensVenda
AS
SELECT  i.IdVenda,
        i.IdProduto,
        p.Nome AS Produto,
        c.Nome AS Categoria,
        i.Quantidade,
        i.PrecoUnitario,
        i.Subtotal
FROM dbo.ItemVenda i
JOIN dbo.Produto   p ON p.IdProduto   = i.IdProduto
JOIN dbo.Categoria c ON c.IdCategoria = p.IdCategoria;
GO

--Teste
SELECT * FROM vw_ItensVenda
GO

-- 5 -> Movimentações do fluxo de caixa, com a origem (venda ou lançamento manual)
-- e a categoria (vendas sem categoria aparecem como 'Vendas').
CREATE OR ALTER VIEW dbo.vw_FluxoCaixa
AS
SELECT  m.IdMovimentacao,
        m.DataMovimentacao,
        m.Descricao,
        m.Tipo,
        m.Valor,
        CASE WHEN m.Tipo = 'Entrada' THEN m.Valor ELSE -m.Valor END AS ValorComSinal,
        CASE WHEN m.IdVenda IS NOT NULL THEN 'Venda' ELSE 'Manual' END AS Origem,
        m.IdVenda,
        m.IdFuncionario,
        pe.Nome AS Funcionario,
        COALESCE(m.Categoria,
                 CASE WHEN m.IdVenda IS NOT NULL THEN 'Vendas'
                      WHEN m.Tipo = 'Entrada'    THEN 'Outras entradas'
                      ELSE 'Outras saídas' END) AS Categoria
FROM dbo.Movimentacao m
LEFT JOIN dbo.Pessoa  pe ON pe.IdPessoa = m.IdFuncionario;
GO

--Teste
SELECT * FROM vw_FluxoCaixa
GO

-- 6 -> Produtos mais vendidos (somente vendas concluídas).
CREATE OR ALTER VIEW dbo.vw_ProdutosMaisVendidos
AS
SELECT  p.IdProduto,
        p.Nome AS Produto,
        c.Nome AS Categoria,
        SUM(i.Quantidade) AS UnidadesVendidas,
        SUM(i.Subtotal)   AS Faturamento
FROM dbo.ItemVenda i
JOIN dbo.Venda     v ON v.IdVenda     = i.IdVenda AND v.Status = 'Concluída'
JOIN dbo.Produto   p ON p.IdProduto   = i.IdProduto
JOIN dbo.Categoria c ON c.IdCategoria = p.IdCategoria
GROUP BY p.IdProduto, p.Nome, c.Nome;
GO

--Teste
SELECT * FROM vw_ProdutosMaisVendidos
GO

-- 7 -> Vendas concluídas por dia (gráfico dos últimos 30 dias do dashboard).
CREATE OR ALTER VIEW dbo.vw_VendasPorDia
AS
SELECT  CAST(v.DataVenda AS DATE) AS Dia,
        COUNT(*)                  AS QtdVendas,
        SUM(v.ValorTotal)         AS Faturamento
FROM dbo.Venda v
WHERE v.Status = 'Concluída'
GROUP BY CAST(v.DataVenda AS DATE);
GO

--Teste
SELECT * FROM vw_VendasPorDia
GO

-- 8 -> Cards do dashboard (uma linha).
CREATE OR ALTER VIEW dbo.vw_Dashboard
AS
SELECT
    (SELECT COUNT(*)  FROM dbo.Venda WHERE Status = 'Concluída' AND CAST(DataVenda AS DATE) = CAST(GETDATE() AS DATE)) AS VendasHoje,
    (SELECT ISNULL(SUM(ValorTotal), 0) FROM dbo.Venda WHERE Status = 'Concluída' AND CAST(DataVenda AS DATE) = CAST(GETDATE() AS DATE)) AS FaturamentoHoje,
    dbo.fn_SaldoCaixaAte(CAST(GETDATE() AS DATE)) AS SaldoCaixa,
    (SELECT COUNT(*) FROM dbo.vw_Estoque WHERE EmAlerta = 1) AS ProdutosEmAlerta,
    (SELECT COUNT(*) FROM dbo.Produto WHERE Ativo = 1) AS ProdutosAtivos,
    (SELECT COUNT(*) FROM dbo.Funcionario WHERE Ativo = 1) AS FuncionariosAtivos;
GO

--Teste
SELECT * FROM vw_Dashboard
GO

-- 9 -> View de Categorias
CREATE OR ALTER VIEW dbo.vw_Categorias AS
SELECT
    IdCategoria,
    Nome,
    Descricao,
    Ativa
FROM dbo.Categoria;
GO

-- Teste
SELECT * FROM dbo.vw_Categorias;
GO

-- 10 -> View de Produtos
CREATE OR ALTER VIEW dbo.vw_Produtos AS
SELECT
    IdProduto,
    IdCategoria,
    Nome,
    Descricao,
    Preco,
    QuantidadeEstoque,
    EstoqueMinimo,
    DataValidade,
    Ativo
FROM dbo.Produto;
GO

-- Teste
SELECT * FROM dbo.vw_Produtos;
GO

-- 11 -> View de Vendas individuais
CREATE OR ALTER VIEW dbo.vw_Vendas_Individuais AS
SELECT
    IdVenda,
    IdFuncionario,
    DataVenda,
    ValorTotal,
    Desconto,
    Status
FROM dbo.Venda;
GO

-- Teste
SELECT * FROM vw_Vendas_Individuais
GO

-- 12 -> View de Movimentações
CREATE OR ALTER VIEW dbo.vw_Movimentacoes AS
SELECT
    IdMovimentacao,
    Tipo,
    Valor,
    Descricao,
    Categoria,
    DataMovimentacao,
    IdFuncionario,
    IdVenda
FROM dbo.Movimentacao;
GO

-- Teste
SELECT * FROM dbo.vw_Movimentacoes;
GO
/* =====================================================================
   6. PROCEDURES
   Erros de regra de negócio usam THROW (50000+) com mensagem em português.
   ===================================================================== */

-- ---------------------------------------------------------------------
-- 1 -> Cadastrar funcionários
-- ---------------------------------------------------------------------
CREATE OR ALTER PROCEDURE dbo.sp_CadastrarFuncionario
    @Nome       VARCHAR(150),
    @Email      VARCHAR(150) = NULL,
    @Perfil     VARCHAR(20)  = 'Funcionario',
    @IdPessoa   INT          OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    SET @Nome  = LTRIM(RTRIM(ISNULL(@Nome, '')));
    SET @Email = NULLIF(LTRIM(RTRIM(ISNULL(@Email, ''))), '');

    IF @Nome = ''
        THROW 50100, 'Informe o nome do funcionário.', 1;

    IF @Perfil NOT IN ('Admin', 'Funcionario')
        THROW 50101, 'Perfil inválido. Use Admin ou Funcionario.', 1;

    IF @Email IS NOT NULL AND EXISTS (SELECT 1 FROM dbo.Pessoa WHERE Email = @Email)
        THROW 50104, 'Já existe uma pessoa cadastrada com esse e-mail.', 1;

    BEGIN TRY
        BEGIN TRAN;

        INSERT dbo.Pessoa (Nome, Email)
        VALUES (@Nome, @Email);

        SET @IdPessoa = SCOPE_IDENTITY();

        INSERT dbo.Funcionario (IdPessoa, Perfil, Ativo)
        VALUES (@IdPessoa, @Perfil, 1);

        COMMIT;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK;
        THROW;
    END CATCH
END;
GO

-- TESTE 1: Cadastrar funcionário
BEGIN TRANSACTION;

DECLARE @IdPessoa INT;
DECLARE @EmailTeste VARCHAR(150) = CONCAT('teste_', CONVERT(VARCHAR(36), NEWID()), '@exemplo.com');

EXEC dbo.sp_CadastrarFuncionario
    @Nome = 'Funcionario Teste',
    @Email = @EmailTeste,
    @Perfil = 'Funcionario',
    @IdPessoa = @IdPessoa OUTPUT;

SELECT *
FROM dbo.Pessoa
WHERE IdPessoa = @IdPessoa;

SELECT *
FROM dbo.Funcionario
WHERE IdPessoa = @IdPessoa;

ROLLBACK TRANSACTION;
GO

-- ---------------------------------------------------------------------
-- 2 -> Atualizar funcionários
-- ---------------------------------------------------------------------
CREATE OR ALTER PROCEDURE dbo.sp_AtualizarFuncionario
    @IdPessoa  INT,
    @Nome      VARCHAR(150),
    @Email     VARCHAR(150) = NULL,
    @Perfil    VARCHAR(20)
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    SET @Nome  = LTRIM(RTRIM(ISNULL(@Nome, '')));
    SET @Email = NULLIF(LTRIM(RTRIM(ISNULL(@Email, ''))), '');

    IF NOT EXISTS (SELECT 1 FROM dbo.Funcionario WHERE IdPessoa = @IdPessoa)
        THROW 50110, 'Funcionário não encontrado.', 1;

    IF @Nome = ''
        THROW 50100, 'Informe o nome do funcionário.', 1;

    IF @Perfil NOT IN ('Admin', 'Funcionario')
        THROW 50101, 'Perfil inválido. Use Admin ou Funcionario.', 1;

    IF @Email IS NOT NULL AND EXISTS (SELECT 1 FROM dbo.Pessoa WHERE Email = @Email AND IdPessoa <> @IdPessoa)
        THROW 50104, 'Já existe uma pessoa cadastrada com esse e-mail.', 1;

    -- não deixa o sistema sem nenhum administrador ativo
    IF @Perfil <> 'Admin'
       AND EXISTS (SELECT 1 FROM dbo.Funcionario WHERE IdPessoa = @IdPessoa AND Perfil = 'Admin' AND Ativo = 1)
       AND NOT EXISTS (SELECT 1 FROM dbo.Funcionario WHERE IdPessoa <> @IdPessoa AND Perfil = 'Admin' AND Ativo = 1)
        THROW 50111, 'O sistema precisa de ao menos um administrador ativo.', 1;

    BEGIN TRY
        BEGIN TRAN;

        UPDATE dbo.Pessoa
           SET Nome = @Nome, Email = @Email
         WHERE IdPessoa = @IdPessoa;

        UPDATE dbo.Funcionario SET Perfil = @Perfil WHERE IdPessoa = @IdPessoa;

        COMMIT;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK;
        THROW;
    END CATCH
END;
GO

-- TESTE 2: Atualizar funcionário
DECLARE @IdPessoa INT;
DECLARE @Nome VARCHAR(150);
DECLARE @Email VARCHAR(150);
DECLARE @Perfil VARCHAR(20);

SELECT TOP 1
    @IdPessoa = IdFuncionario,
    @Nome = Nome,
    @Email = Email,
    @Perfil = Perfil
FROM dbo.vw_Funcionarios
WHERE Perfil = 'Funcionario'
  AND Ativo = 1;

IF @IdPessoa IS NULL
BEGIN
    PRINT 'Teste ignorado: não existe funcionário ativo para testar.';
END
ELSE
BEGIN
    BEGIN TRANSACTION;

    SET @Nome = CONCAT(@Nome, ' Teste');

    EXEC dbo.sp_AtualizarFuncionario
        @IdPessoa = @IdPessoa,
        @Nome = @Nome,
        @Email = @Email,
        @Perfil = @Perfil;

    SELECT *
    FROM dbo.vw_Funcionarios
    WHERE IdFuncionario = @IdPessoa;

    ROLLBACK TRANSACTION;
END;
GO
-- ---------------------------------------------------------------------
-- 3 -> Botão do "olho": deixa de exibir / reativa funcionario
-- ---------------------------------------------------------------------
-- 
CREATE OR ALTER PROCEDURE dbo.sp_AlternarStatusFuncionario
    @IdFuncionario INT
AS
BEGIN
    SET NOCOUNT ON;

    IF NOT EXISTS (SELECT 1 FROM dbo.Funcionario WHERE IdPessoa = @IdFuncionario)
        THROW 50110, 'Funcionário não encontrado.', 1;

    IF EXISTS (SELECT 1 FROM dbo.Funcionario WHERE IdPessoa = @IdFuncionario AND Perfil = 'Admin' AND Ativo = 1)
       AND NOT EXISTS (SELECT 1 FROM dbo.Funcionario WHERE IdPessoa <> @IdFuncionario AND Perfil = 'Admin' AND Ativo = 1)
        THROW 50111, 'O sistema precisa de ao menos um administrador ativo.', 1;

    UPDATE dbo.Funcionario
       SET Ativo = CASE WHEN Ativo = 1 THEN 0 ELSE 1 END
     WHERE IdPessoa = @IdFuncionario;

    SELECT Ativo FROM dbo.Funcionario WHERE IdPessoa = @IdFuncionario;
END;
GO

-- TESTE 3: Alternar status do funcionário
DECLARE @IdFuncionario INT;

SELECT TOP 1 @IdFuncionario = IdPessoa
FROM dbo.Funcionario
WHERE Perfil = 'Funcionario';

IF @IdFuncionario IS NULL
BEGIN
    PRINT 'Teste ignorado: não existe funcionário para testar.';
END
ELSE
BEGIN
    BEGIN TRANSACTION;

    EXEC dbo.sp_AlternarStatusFuncionario
        @IdFuncionario = @IdFuncionario;

    SELECT IdPessoa, Perfil, Ativo
    FROM dbo.Funcionario
    WHERE IdPessoa = @IdFuncionario;

    ROLLBACK TRANSACTION;
END;
GO

-- ---------------------------------------------------------------------
-- 4 -> -- Excluir: só se o funcionário nunca registrou venda nem movimentação.
-- ---------------------------------------------------------------------

CREATE OR ALTER PROCEDURE dbo.sp_ExcluirFuncionario
    @IdFuncionario INT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    IF NOT EXISTS (SELECT 1 FROM dbo.Funcionario WHERE IdPessoa = @IdFuncionario)
        THROW 50110, 'Funcionário não encontrado.', 1;

    IF EXISTS (SELECT 1 FROM dbo.Venda WHERE IdFuncionario = @IdFuncionario)
       OR EXISTS (SELECT 1 FROM dbo.Movimentacao WHERE IdFuncionario = @IdFuncionario)
        THROW 50112, 'Funcionário com vendas ou movimentações não pode ser excluído. Use "deixar de exibir".', 1;

    IF EXISTS (SELECT 1 FROM dbo.Funcionario WHERE IdPessoa = @IdFuncionario AND Perfil = 'Admin' AND Ativo = 1)
       AND NOT EXISTS (SELECT 1 FROM dbo.Funcionario WHERE IdPessoa <> @IdFuncionario AND Perfil = 'Admin' AND Ativo = 1)
        THROW 50111, 'O sistema precisa de ao menos um administrador ativo.', 1;

    BEGIN TRY
        BEGIN TRAN;
        DELETE dbo.Funcionario WHERE IdPessoa = @IdFuncionario;
        DELETE dbo.Pessoa      WHERE IdPessoa = @IdFuncionario;
        COMMIT;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK;
        THROW;
    END CATCH
END;
GO
-- TESTE 4: Excluir funcionário de teste
BEGIN TRANSACTION;

DECLARE @IdPessoa INT;
DECLARE @EmailTeste VARCHAR(150) = CONCAT('excluir_', CONVERT(VARCHAR(36), NEWID()), '@exemplo.com');

EXEC dbo.sp_CadastrarFuncionario
    @Nome = 'Funcionario Para Excluir',
    @Email = @EmailTeste,
    @Perfil = 'Funcionario',
    @IdPessoa = @IdPessoa OUTPUT;

EXEC dbo.sp_ExcluirFuncionario
    @IdFuncionario = @IdPessoa;

SELECT *
FROM dbo.Funcionario
WHERE IdPessoa = @IdPessoa;

ROLLBACK TRANSACTION;
GO


-- ---------------------------------------------------------------------
-- 5 -> Lista de cadastros (busca + situação + ordenação).
-- @Ordenar: nome | perfil | status ; @Direcao: asc | desc
-- ---------------------------------------------------------------------

CREATE OR ALTER PROCEDURE dbo.sp_ListarFuncionarios
    @Busca     VARCHAR(150) = NULL,
    @Situacao  VARCHAR(10)  = NULL,       -- 'ativo' | 'inativo'
    @Ordenar   VARCHAR(20)  = 'nome',
    @Direcao   VARCHAR(4)   = 'asc'
AS
BEGIN
    SET NOCOUNT ON;

    IF @Direcao NOT IN ('asc', 'desc') SET @Direcao = 'asc';
    SET @Busca = NULLIF(LTRIM(RTRIM(ISNULL(@Busca, ''))), '');

    SELECT *
    FROM dbo.vw_Funcionarios
    WHERE (@Busca IS NULL OR Nome  LIKE '%' + @Busca + '%'
                          OR Email LIKE '%' + @Busca + '%')
      AND (@Situacao IS NULL
           OR (@Situacao = 'ativo'   AND Ativo = 1)
           OR (@Situacao = 'inativo' AND Ativo = 0))
    ORDER BY
        CASE WHEN @Ordenar = 'perfil' AND @Direcao = 'asc'  THEN Perfil END ASC,
        CASE WHEN @Ordenar = 'perfil' AND @Direcao = 'desc' THEN Perfil END DESC,
        CASE WHEN @Ordenar = 'status' AND @Direcao = 'asc'  THEN CASE WHEN Ativo = 1 THEN 0 ELSE 1 END END ASC,
        CASE WHEN @Ordenar = 'status' AND @Direcao = 'desc' THEN CASE WHEN Ativo = 1 THEN 0 ELSE 1 END END DESC,
        CASE WHEN @Ordenar NOT IN ('perfil', 'status') AND @Direcao = 'desc' THEN Nome END DESC,
        Nome ASC;
END;
GO

-- TESTE 5: Listar funcionários
EXEC dbo.sp_ListarFuncionarios;
GO

-- Teste com filtros e ordenação
EXEC dbo.sp_ListarFuncionarios
    @Busca = NULL,
    @Situacao = 'ativo',
    @Ordenar = 'nome',
    @Direcao = 'asc';
GO

-- ---------------------------------------------------------------------
-- 6 -> Cadastrar categorias
-- ---------------------------------------------------------------------
CREATE OR ALTER PROCEDURE dbo.sp_CadastrarCategoria
    @Nome         VARCHAR(80),
    @Descricao    VARCHAR(250) = NULL,
    @IdCategoria  INT          OUTPUT
AS
BEGIN
    SET NOCOUNT ON;

    SET @Nome = LTRIM(RTRIM(ISNULL(@Nome, '')));

    IF @Nome = ''
        THROW 50200, 'Informe o nome da categoria.', 1;

    IF EXISTS (SELECT 1 FROM dbo.Categoria WHERE Nome = @Nome)
        THROW 50201, 'Já existe uma categoria com esse nome.', 1;

    INSERT dbo.Categoria (Nome, Descricao, Ativa) VALUES (@Nome, @Descricao, 1);
    SET @IdCategoria = SCOPE_IDENTITY();
END;
GO

-- TESTE 6: Cadastrar categoria
BEGIN TRANSACTION;

DECLARE @IdCategoria INT;
DECLARE @NomeCategoriaTeste VARCHAR(80) = CONCAT('Categoria Teste ', LEFT(CONVERT(VARCHAR(36), NEWID()), 8));

EXEC dbo.sp_CadastrarCategoria
    @Nome = @NomeCategoriaTeste,
    @Descricao = 'Categoria criada para teste',
    @IdCategoria = @IdCategoria OUTPUT;

SELECT *
FROM dbo.Categoria
WHERE IdCategoria = @IdCategoria;

ROLLBACK TRANSACTION;
GO

-- ---------------------------------------------------------------------
-- 7 -> -- Cadastrar produto
-- ---------------------------------------------------------------------

CREATE OR ALTER PROCEDURE dbo.sp_CadastrarProduto
    @IdCategoria        INT,
    @Nome               VARCHAR(150),
    @Descricao          VARCHAR(500) = NULL,
    @Preco              DECIMAL(18,2),
    @QuantidadeEstoque  INT          = 0,
    @EstoqueMinimo      INT          = 20,
    @DataValidade       DATE         = NULL,
    @IdProduto          INT          OUTPUT
AS
BEGIN
    SET NOCOUNT ON;

    SET @Nome = LTRIM(RTRIM(ISNULL(@Nome, '')));

    IF @Nome = ''
        THROW 50210, 'Informe o nome do produto.', 1;
    IF NOT EXISTS (SELECT 1 FROM dbo.Categoria WHERE IdCategoria = @IdCategoria AND Ativa = 1)
        THROW 50211, 'Categoria inválida ou inativa.', 1;
    IF @Preco < 0
        THROW 50212, 'O preço não pode ser negativo.', 1;
    IF @QuantidadeEstoque < 0
        THROW 50213, 'A quantidade não pode ser negativa.', 1;
    IF @EstoqueMinimo < 0
        THROW 50214, 'O estoque mínimo não pode ser negativo.', 1;

    INSERT dbo.Produto (IdCategoria, Nome, Descricao, Preco, QuantidadeEstoque, EstoqueMinimo, DataValidade, Ativo)
    VALUES (@IdCategoria, @Nome, @Descricao, @Preco, @QuantidadeEstoque, @EstoqueMinimo, @DataValidade, 1);

    SET @IdProduto = SCOPE_IDENTITY();
END;
GO

-- TESTE 7: Cadastrar produto
DECLARE @IdCategoria INT;

SELECT TOP 1 @IdCategoria = IdCategoria
FROM dbo.Categoria
WHERE Ativa = 1;

IF @IdCategoria IS NULL
BEGIN
    PRINT 'Teste ignorado: não existe categoria ativa.';
END
ELSE
BEGIN
    BEGIN TRANSACTION;

    DECLARE @IdProduto INT;

    EXEC dbo.sp_CadastrarProduto
        @IdCategoria = @IdCategoria,
        @Nome = 'Produto Teste',
        @Descricao = 'Produto criado para teste',
        @Preco = 10.00,
        @QuantidadeEstoque = 5,
        @EstoqueMinimo = 2,
        @DataValidade = NULL,
        @IdProduto = @IdProduto OUTPUT;

    SELECT *
    FROM dbo.Produto
    WHERE IdProduto = @IdProduto;

    ROLLBACK TRANSACTION;
END;
GO

-- ---------------------------------------------------------------------
-- 8 -> -- Atualizar produto
-- ---------------------------------------------------------------------
CREATE OR ALTER PROCEDURE dbo.sp_AtualizarProduto
    @IdProduto          INT,
    @IdCategoria        INT,
    @Nome               VARCHAR(150),
    @Descricao          VARCHAR(500) = NULL,
    @Preco              DECIMAL(18,2),
    @QuantidadeEstoque  INT,
    @EstoqueMinimo      INT,
    @DataValidade       DATE         = NULL,
    @Ativo              BIT
AS
BEGIN
    SET NOCOUNT ON;

    SET @Nome = LTRIM(RTRIM(ISNULL(@Nome, '')));

    IF NOT EXISTS (SELECT 1 FROM dbo.Produto WHERE IdProduto = @IdProduto)
        THROW 50215, 'Produto não encontrado.', 1;
    IF @Nome = ''
        THROW 50210, 'Informe o nome do produto.', 1;
    IF NOT EXISTS (SELECT 1 FROM dbo.Categoria WHERE IdCategoria = @IdCategoria)
        THROW 50211, 'Categoria inválida.', 1;
    IF @Preco < 0
        THROW 50212, 'O preço não pode ser negativo.', 1;
    IF @QuantidadeEstoque < 0
        THROW 50213, 'A quantidade não pode ser negativa.', 1;
    IF @EstoqueMinimo < 0
        THROW 50214, 'O estoque mínimo não pode ser negativo.', 1;

    UPDATE dbo.Produto
       SET IdCategoria = @IdCategoria, Nome = @Nome, Descricao = @Descricao, Preco = @Preco,
           QuantidadeEstoque = @QuantidadeEstoque, EstoqueMinimo = @EstoqueMinimo,
           DataValidade = @DataValidade, Ativo = @Ativo
     WHERE IdProduto = @IdProduto;
END;
GO

-- TESTE 8: Atualizar produto
DECLARE @IdProduto INT;
DECLARE @IdCategoria INT;
DECLARE @Nome VARCHAR(150);
DECLARE @Descricao VARCHAR(500);
DECLARE @Preco DECIMAL(18,2);
DECLARE @QuantidadeEstoque INT;
DECLARE @EstoqueMinimo INT;
DECLARE @DataValidade DATE;
DECLARE @Ativo BIT;

SELECT TOP 1
    @IdProduto = IdProduto,
    @IdCategoria = IdCategoria,
    @Nome = Nome,
    @Descricao = Descricao,
    @Preco = Preco,
    @QuantidadeEstoque = QuantidadeEstoque,
    @EstoqueMinimo = EstoqueMinimo,
    @DataValidade = DataValidade,
    @Ativo = Ativo
FROM dbo.Produto;

IF @IdProduto IS NULL
BEGIN
    PRINT 'Teste ignorado: não existe produto para atualizar.';
END
ELSE
BEGIN
    BEGIN TRANSACTION;

    EXEC dbo.sp_AtualizarProduto
        @IdProduto = @IdProduto,
        @IdCategoria = @IdCategoria,
        @Nome = @Nome,
        @Descricao = @Descricao,
        @Preco = @Preco,
        @QuantidadeEstoque = @QuantidadeEstoque,
        @EstoqueMinimo = @EstoqueMinimo,
        @DataValidade = @DataValidade,
        @Ativo = @Ativo;

    SELECT *
    FROM dbo.Produto
    WHERE IdProduto = @IdProduto;

    ROLLBACK TRANSACTION;
END;
GO
-- ---------------------------------------------------------------------
-- 9 -> Botão do "olho" do estoque: desativado deixa de aparecer no caixa.
-- ---------------------------------------------------------------------
CREATE OR ALTER PROCEDURE dbo.sp_AlternarAtivoProduto
    @IdProduto INT
AS
BEGIN
    SET NOCOUNT ON;

    IF NOT EXISTS (SELECT 1 FROM dbo.Produto WHERE IdProduto = @IdProduto)
        THROW 50215, 'Produto não encontrado.', 1;

    UPDATE dbo.Produto
       SET Ativo = CASE WHEN Ativo = 1 THEN 0 ELSE 1 END
     WHERE IdProduto = @IdProduto;

    SELECT Ativo FROM dbo.Produto WHERE IdProduto = @IdProduto;
END;
GO

-- TESTE 9: Alternar status do produto
DECLARE @IdProduto INT;

SELECT TOP 1 @IdProduto = IdProduto
FROM dbo.Produto;

IF @IdProduto IS NULL
BEGIN
    PRINT 'Teste ignorado: não existe produto para testar.';
END
ELSE
BEGIN
    BEGIN TRANSACTION;

    EXEC dbo.sp_AlternarAtivoProduto
        @IdProduto = @IdProduto;

    SELECT IdProduto, Nome, Ativo
    FROM dbo.Produto
    WHERE IdProduto = @IdProduto;

    ROLLBACK TRANSACTION;
END;
GO

-- ---------------------------------------------------------------------
-- 10 -> Produto já vendido não pode ser excluído (só desativado).
-- ---------------------------------------------------------------------
CREATE OR ALTER PROCEDURE dbo.sp_ExcluirProduto
    @IdProduto INT
AS
BEGIN
    SET NOCOUNT ON;

    IF NOT EXISTS (SELECT 1 FROM dbo.Produto WHERE IdProduto = @IdProduto)
        THROW 50215, 'Produto não encontrado.', 1;

    IF EXISTS (SELECT 1 FROM dbo.ItemVenda WHERE IdProduto = @IdProduto)
        THROW 50216, 'Produto com vendas não pode ser excluído. Desative-o para tirar do caixa.', 1;

    DELETE dbo.Produto WHERE IdProduto = @IdProduto;
END;
GO

-- TESTE 10: Excluir produto de teste
DECLARE @IdCategoria INT;

SELECT TOP 1 @IdCategoria = IdCategoria
FROM dbo.Categoria;

IF @IdCategoria IS NULL
BEGIN
    PRINT 'Teste ignorado: não existe categoria para cadastrar o produto.';
END
ELSE
BEGIN
    BEGIN TRANSACTION;

    DECLARE @IdProduto INT;

    EXEC dbo.sp_CadastrarProduto
        @IdCategoria = @IdCategoria,
        @Nome = 'Produto Para Excluir',
        @Descricao = 'Produto criado para teste de exclusão',
        @Preco = 5.00,
        @QuantidadeEstoque = 1,
        @EstoqueMinimo = 0,
        @DataValidade = NULL,
        @IdProduto = @IdProduto OUTPUT;

    EXEC dbo.sp_ExcluirProduto
        @IdProduto = @IdProduto;

    SELECT *
    FROM dbo.Produto
    WHERE IdProduto = @IdProduto;

    ROLLBACK TRANSACTION;
END;
GO

-- ---------------------------------------------------------------------
-- 11 -> -- Estoque: busca, categoria, situação e ordenação.
-- @Situacao: baixo | sem | proxima | vencido | inativo
-- @Ordenar : produto | categoria | preco | estoque | validade | status
-- ---------------------------------------------------------------------

CREATE OR ALTER PROCEDURE dbo.sp_ListarEstoque
    @Busca        VARCHAR(150) = NULL,
    @IdCategoria  INT          = NULL,
    @Situacao     VARCHAR(10)  = NULL,
    @Ordenar      VARCHAR(20)  = 'produto',
    @Direcao      VARCHAR(4)   = 'asc'
AS
BEGIN
    SET NOCOUNT ON;

    IF @Direcao NOT IN ('asc', 'desc') SET @Direcao = 'asc';
    SET @Busca = NULLIF(LTRIM(RTRIM(ISNULL(@Busca, ''))), '');

    SELECT *
    FROM dbo.vw_Estoque
    WHERE (@Busca IS NULL OR Nome LIKE '%' + @Busca + '%')
      AND (@IdCategoria IS NULL OR IdCategoria = @IdCategoria)
      AND (@Situacao IS NULL
           OR (@Situacao = 'baixo'   AND EstoqueBaixo = 1)
           OR (@Situacao = 'sem'     AND SemEstoque = 1)
           OR (@Situacao = 'proxima' AND ValidadeProxima = 1)
           OR (@Situacao = 'vencido' AND Vencido = 1)
           OR (@Situacao = 'inativo' AND Ativo = 0))
    ORDER BY
        -- produtos sem validade ficam sempre no final
        CASE WHEN @Ordenar = 'validade' AND DataValidade IS NULL THEN 1 ELSE 0 END ASC,
        CASE WHEN @Ordenar = 'validade' AND @Direcao = 'asc'  THEN DataValidade END ASC,
        CASE WHEN @Ordenar = 'validade' AND @Direcao = 'desc' THEN DataValidade END DESC,
        CASE WHEN @Ordenar = 'categoria' AND @Direcao = 'asc'  THEN Categoria END ASC,
        CASE WHEN @Ordenar = 'categoria' AND @Direcao = 'desc' THEN Categoria END DESC,
        CASE WHEN @Ordenar = 'preco'   AND @Direcao = 'asc'  THEN Preco END ASC,
        CASE WHEN @Ordenar = 'preco'   AND @Direcao = 'desc' THEN Preco END DESC,
        CASE WHEN @Ordenar = 'estoque' AND @Direcao = 'asc'  THEN Estoque END ASC,
        CASE WHEN @Ordenar = 'estoque' AND @Direcao = 'desc' THEN Estoque END DESC,
        CASE WHEN @Ordenar = 'status' AND @Direcao = 'asc'  THEN CASE WHEN Ativo = 1 THEN 0 ELSE 1 END END ASC,
        CASE WHEN @Ordenar = 'status' AND @Direcao = 'desc' THEN CASE WHEN Ativo = 1 THEN 0 ELSE 1 END END DESC,
        CASE WHEN @Ordenar NOT IN ('validade', 'categoria', 'preco', 'estoque', 'status')
                  AND @Direcao = 'desc' THEN Nome END DESC,
        Nome ASC;
END;
GO

-- TESTE 11: Listar estoque
EXEC dbo.sp_ListarEstoque;
GO

-- Teste filtrando produtos com estoque baixo
EXEC dbo.sp_ListarEstoque
    @Busca = NULL,
    @IdCategoria = NULL,
    @Situacao = 'baixo',
    @Ordenar = 'estoque',
    @Direcao = 'asc';
GO

-- ---------------------------------------------------------------------
-- 12 -> Indicadores do topo da tela de estoque (sobre os produtos ativos).
-- ---------------------------------------------------------------------
CREATE OR ALTER PROCEDURE dbo.sp_ResumoEstoque
AS
BEGIN
    SET NOCOUNT ON;

    SELECT  COUNT(*)                                  AS ProdutosAtivos,
            ISNULL(SUM(Estoque), 0)                   AS UnidadesEmEstoque,
            ISNULL(SUM(ValorEmEstoque), 0)            AS ValorEmEstoque,
            ISNULL(SUM(CAST(EmAlerta AS INT)), 0)     AS ProdutosEmAlerta
    FROM dbo.vw_Estoque
    WHERE Ativo = 1;
END;
GO

-- TESTE 12: Consultar resumo do estoque
EXEC dbo.sp_ResumoEstoque;
GO

-- ---------------------------------------------------------------------
-- 13 -> -- ---------------------------------------------------------------------
-- Vendas (Caixa / PDV)
-- Preços vêm SEMPRE do banco. Tudo em uma transação:
-- valida, grava venda e itens, baixa o estoque e gera a entrada no caixa.
-- ---------------------------------------------------------------------
-- ---------------------------------------------------------------------

CREATE OR ALTER PROCEDURE dbo.sp_RegistrarVenda
    @IdFuncionario  INT,
    @Itens          dbo.TipoItemVenda READONLY,
    @Desconto       DECIMAL(18,2) = 0,
    @IdVenda        INT           OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    DECLARE @Agrupado TABLE (IdProduto INT PRIMARY KEY, Quantidade INT NOT NULL);
    DECLARE @Subtotal DECIMAL(18,2), @Total DECIMAL(18,2), @Msg NVARCHAR(200);

    SET @Desconto = ISNULL(@Desconto, 0);

    IF NOT EXISTS (SELECT 1 FROM dbo.Funcionario WHERE IdPessoa = @IdFuncionario AND Ativo = 1)
        THROW 50300, 'Funcionário inexistente ou inativo.', 1;

    -- junta itens repetidos e descarta quantidades inválidas
    INSERT @Agrupado (IdProduto, Quantidade)
    SELECT IdProduto, SUM(Quantidade)
    FROM @Itens
    WHERE Quantidade > 0
    GROUP BY IdProduto;

    IF NOT EXISTS (SELECT 1 FROM @Agrupado)
        THROW 50301, 'Adicione ao menos um produto.', 1;

    IF @Desconto < 0
        THROW 50302, 'O desconto não pode ser negativo.', 1;

    BEGIN TRY
        BEGIN TRAN;

        -- trava as linhas dos produtos para ninguém vender o mesmo estoque ao mesmo tempo
        IF EXISTS (SELECT 1
                   FROM @Agrupado a
                   LEFT JOIN dbo.Produto p WITH (UPDLOCK, HOLDLOCK) ON p.IdProduto = a.IdProduto
                   WHERE p.IdProduto IS NULL OR p.Ativo = 0)
            THROW 50303, 'Produto não encontrado ou inativo.', 1;

        SELECT TOP 1 @Msg = CONCAT('Estoque insuficiente para ', p.Nome, ' (disponível: ', p.QuantidadeEstoque, ').')
        FROM @Agrupado a
        JOIN dbo.Produto p ON p.IdProduto = a.IdProduto
        WHERE a.Quantidade > p.QuantidadeEstoque;

        IF @Msg IS NOT NULL
            THROW 50304, @Msg, 1;

        SELECT @Subtotal = SUM(p.Preco * a.Quantidade)
        FROM @Agrupado a
        JOIN dbo.Produto p ON p.IdProduto = a.IdProduto;

        IF @Desconto > @Subtotal
            THROW 50305, 'O desconto não pode ser maior que o subtotal.', 1;

        SET @Total = @Subtotal - @Desconto;

        INSERT dbo.Venda (IdFuncionario, DataVenda, ValorTotal, Desconto, Status)
        VALUES (@IdFuncionario, GETDATE(), @Total, @Desconto, 'Concluída');

        SET @IdVenda = SCOPE_IDENTITY();

        INSERT dbo.ItemVenda (IdVenda, IdProduto, Quantidade, PrecoUnitario)
        SELECT @IdVenda, a.IdProduto, a.Quantidade, p.Preco
        FROM @Agrupado a
        JOIN dbo.Produto p ON p.IdProduto = a.IdProduto;

        UPDATE p
           SET p.QuantidadeEstoque = p.QuantidadeEstoque - a.Quantidade
        FROM dbo.Produto p
        JOIN @Agrupado a ON a.IdProduto = p.IdProduto;

        -- a venda concluída entra automaticamente no fluxo de caixa
        IF @Total > 0
            INSERT dbo.Movimentacao (Tipo, Valor, Descricao, DataMovimentacao, IdFuncionario, IdVenda)
            VALUES ('Entrada', @Total, CONCAT('Venda #', @IdVenda), GETDATE(), @IdFuncionario, @IdVenda);

        COMMIT;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK;
        THROW;
    END CATCH
END;
GO

-- TESTE 13: Registrar venda
DECLARE @IdFuncionario INT;
DECLARE @IdProduto INT;
DECLARE @IdVenda INT;
DECLARE @Itens dbo.TipoItemVenda;

SELECT TOP 1 @IdFuncionario = IdPessoa
FROM dbo.Funcionario
WHERE Ativo = 1;

SELECT TOP 1 @IdProduto = IdProduto
FROM dbo.Produto
WHERE Ativo = 1
  AND QuantidadeEstoque > 0;

IF @IdFuncionario IS NULL OR @IdProduto IS NULL
BEGIN
    PRINT 'Teste ignorado: é necessário ter funcionário ativo e produto com estoque.';
END
ELSE
BEGIN
    BEGIN TRANSACTION;

    INSERT INTO @Itens (IdProduto, Quantidade)
    VALUES (@IdProduto, 1);

    EXEC dbo.sp_RegistrarVenda
        @IdFuncionario = @IdFuncionario,
        @Itens = @Itens,
        @Desconto = 0,
        @IdVenda = @IdVenda OUTPUT;

    SELECT *
    FROM dbo.Venda
    WHERE IdVenda = @IdVenda;

    SELECT *
    FROM dbo.ItemVenda
    WHERE IdVenda = @IdVenda;

    SELECT *
    FROM dbo.Movimentacao
    WHERE IdVenda = @IdVenda;

    ROLLBACK TRANSACTION;
END;
GO

-- ---------------------------------------------------------------------
-- 14 -> -- Cancelar: devolve o estoque e remove a entrada que a venda gerou no caixa.
-- ---------------------------------------------------------------------
-- ---------------------------------------------------------------------

CREATE OR ALTER PROCEDURE dbo.sp_CancelarVenda
    @IdVenda INT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    IF NOT EXISTS (SELECT 1 FROM dbo.Venda WHERE IdVenda = @IdVenda)
        THROW 50310, 'Venda não encontrada.', 1;

    IF NOT EXISTS (SELECT 1 FROM dbo.Venda WHERE IdVenda = @IdVenda AND Status = 'Concluída')
        THROW 50311, 'Somente vendas concluídas podem ser canceladas.', 1;

    BEGIN TRY
        BEGIN TRAN;

        UPDATE dbo.Venda SET Status = 'Cancelada' WHERE IdVenda = @IdVenda;

        UPDATE p
           SET p.QuantidadeEstoque = p.QuantidadeEstoque + i.Quantidade
        FROM dbo.Produto   p
        JOIN dbo.ItemVenda i ON i.IdProduto = p.IdProduto
        WHERE i.IdVenda = @IdVenda;

        DELETE dbo.Movimentacao WHERE IdVenda = @IdVenda;

        COMMIT;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK;
        THROW;
    END CATCH
END;
GO

-- TESTE 14: Cancelar venda
DECLARE @IdFuncionario INT;
DECLARE @IdProduto INT;
DECLARE @IdVenda INT;
DECLARE @Itens dbo.TipoItemVenda;

SELECT TOP 1 @IdFuncionario = IdPessoa
FROM dbo.Funcionario
WHERE Ativo = 1;

SELECT TOP 1 @IdProduto = IdProduto
FROM dbo.Produto
WHERE Ativo = 1
  AND QuantidadeEstoque > 0;

IF @IdFuncionario IS NULL OR @IdProduto IS NULL
BEGIN
    PRINT 'Teste ignorado: é necessário ter funcionário ativo e produto com estoque.';
END
ELSE
BEGIN
    BEGIN TRANSACTION;

    INSERT INTO @Itens (IdProduto, Quantidade)
    VALUES (@IdProduto, 1);

    EXEC dbo.sp_RegistrarVenda
        @IdFuncionario = @IdFuncionario,
        @Itens = @Itens,
        @Desconto = 0,
        @IdVenda = @IdVenda OUTPUT;

    EXEC dbo.sp_CancelarVenda
        @IdVenda = @IdVenda;

    SELECT IdVenda, Status, ValorTotal
    FROM dbo.Venda
    WHERE IdVenda = @IdVenda;

    SELECT *
    FROM dbo.Movimentacao
    WHERE IdVenda = @IdVenda;

    ROLLBACK TRANSACTION;
END;
GO

-- ---------------------------------------------------------------------
-- 15 -> -- Tela Vendas: filtros (datas inclusivas, status) + ordenação.
-- @Ordenar: venda | data | funcionario | itens | desconto | valor | status
-- Padrão: data decrescente. Devolve também o total das vendas concluídas listadas.
-- ---------------------------------------------------------------------
-- ---------------------------------------------------------------------


CREATE OR ALTER PROCEDURE dbo.sp_ListarVendas
    @DataInicial       DATE          = NULL,
    @DataFinal         DATE          = NULL,
    @Status            VARCHAR(15)   = NULL,
    @Ordenar           VARCHAR(20)   = 'data',
    @Direcao           VARCHAR(4)    = 'desc',
    @TotalConcluidas   DECIMAL(18,2) = NULL OUTPUT
AS
BEGIN
    SET NOCOUNT ON;

    IF @DataInicial IS NOT NULL AND @DataFinal IS NOT NULL AND @DataInicial > @DataFinal
        THROW 50320, 'A data inicial não pode ser maior que a data final.', 1;

    IF @Direcao NOT IN ('asc', 'desc') SET @Direcao = 'desc';
    SET @Status = NULLIF(LTRIM(RTRIM(ISNULL(@Status, ''))), '');

    DECLARE @Ini DATETIME = CAST(@DataInicial AS DATETIME);
    DECLARE @Fim DATETIME = DATEADD(DAY, 1, CAST(@DataFinal AS DATETIME));

    SELECT @TotalConcluidas = ISNULL(SUM(ValorTotal), 0)
    FROM dbo.vw_Vendas
    WHERE Status = 'Concluída'
      AND (@Status IS NULL OR Status = @Status)
      AND (@Ini IS NULL OR DataVenda >= @Ini)
      AND (@Fim IS NULL OR DataVenda <  @Fim);

    SELECT *
    FROM dbo.vw_Vendas
    WHERE (@Status IS NULL OR Status = @Status)
      AND (@Ini IS NULL OR DataVenda >= @Ini)
      AND (@Fim IS NULL OR DataVenda <  @Fim)
    ORDER BY
        CASE WHEN @Ordenar = 'venda'       AND @Direcao = 'asc'  THEN IdVenda END ASC,
        CASE WHEN @Ordenar = 'venda'       AND @Direcao = 'desc' THEN IdVenda END DESC,
        CASE WHEN @Ordenar = 'funcionario' AND @Direcao = 'asc'  THEN Funcionario END ASC,
        CASE WHEN @Ordenar = 'funcionario' AND @Direcao = 'desc' THEN Funcionario END DESC,
        CASE WHEN @Ordenar = 'itens'       AND @Direcao = 'asc'  THEN QtdItens END ASC,
        CASE WHEN @Ordenar = 'itens'       AND @Direcao = 'desc' THEN QtdItens END DESC,
        CASE WHEN @Ordenar = 'desconto'    AND @Direcao = 'asc'  THEN Desconto END ASC,
        CASE WHEN @Ordenar = 'desconto'    AND @Direcao = 'desc' THEN Desconto END DESC,
        CASE WHEN @Ordenar = 'valor'       AND @Direcao = 'asc'  THEN ValorTotal END ASC,
        CASE WHEN @Ordenar = 'valor'       AND @Direcao = 'desc' THEN ValorTotal END DESC,
        CASE WHEN @Ordenar = 'status'      AND @Direcao = 'asc'  THEN Status END ASC,
        CASE WHEN @Ordenar = 'status'      AND @Direcao = 'desc' THEN Status END DESC,
        CASE WHEN @Ordenar NOT IN ('venda', 'funcionario', 'itens', 'desconto', 'valor', 'status')
                  AND @Direcao = 'asc'  THEN DataVenda END ASC,
        CASE WHEN @Ordenar NOT IN ('venda', 'funcionario', 'itens', 'desconto', 'valor', 'status')
                  AND @Direcao = 'desc' THEN DataVenda END DESC,
        IdVenda DESC;
END;
GO

-- TESTE 15: Listar vendas
DECLARE @TotalConcluidas DECIMAL(18,2);

EXEC dbo.sp_ListarVendas
    @DataInicial = NULL,
    @DataFinal = NULL,
    @Status = NULL,
    @Ordenar = 'data',
    @Direcao = 'desc',
    @TotalConcluidas = @TotalConcluidas OUTPUT;

SELECT @TotalConcluidas AS TotalVendasConcluidas;
GO

-- ---------------------------------------------------------------------
-- 16 -> -- -- Detalhes da venda: cabeçalho + itens.
-- ---------------------------------------------------------------------

CREATE OR ALTER PROCEDURE dbo.sp_DetalhesVenda
    @IdVenda INT
AS
BEGIN
    SET NOCOUNT ON;

    IF NOT EXISTS (SELECT 1 FROM dbo.Venda WHERE IdVenda = @IdVenda)
        THROW 50310, 'Venda não encontrada.', 1;

    SELECT * FROM dbo.vw_Vendas WHERE IdVenda = @IdVenda;
    SELECT * FROM dbo.fn_ItensDaVenda(@IdVenda) ORDER BY Produto;
END;
GO

-- TESTE 16: Consultar detalhes de uma venda
DECLARE @IdVenda INT;

SELECT TOP 1 @IdVenda = IdVenda
FROM dbo.Venda
ORDER BY IdVenda DESC;

IF @IdVenda IS NULL
BEGIN
    PRINT 'Teste ignorado: não existe venda cadastrada.';
END
ELSE
BEGIN
    EXEC dbo.sp_DetalhesVenda
        @IdVenda = @IdVenda;
END;
GO

-- ---------------------------------------------------------------------
-- 17 -> Lançamento manual ("Nova movimentação"), agora com categoria.
-- ---------------------------------------------------------------------
CREATE OR ALTER PROCEDURE dbo.sp_RegistrarMovimentacao
    @Tipo            VARCHAR(10),
    @Valor           DECIMAL(18,2),
    @Descricao       VARCHAR(200),
    @Categoria       VARCHAR(50) = NULL,
    @Data            DATETIME    = NULL,
    @IdFuncionario   INT         = NULL,
    @IdMovimentacao  INT         OUTPUT
AS
BEGIN
    SET NOCOUNT ON;

    SET @Descricao = LTRIM(RTRIM(ISNULL(@Descricao, '')));
    SET @Categoria = NULLIF(LTRIM(RTRIM(ISNULL(@Categoria, ''))), '');
    SET @Data = ISNULL(@Data, GETDATE());

    IF @Tipo NOT IN ('Entrada', 'Saída')
        THROW 50400, 'Tipo inválido. Use Entrada ou Saída.', 1;
    IF @Descricao = ''
        THROW 50401, 'Informe a descrição.', 1;
    IF @Valor IS NULL OR @Valor <= 0
        THROW 50402, 'Informe um valor maior que zero.', 1;
    IF @IdFuncionario IS NOT NULL AND NOT EXISTS (SELECT 1 FROM dbo.Funcionario WHERE IdPessoa = @IdFuncionario)
        THROW 50403, 'Funcionário não encontrado.', 1;

    INSERT dbo.Movimentacao (Tipo, Valor, Descricao, Categoria, DataMovimentacao, IdFuncionario, IdVenda)
    VALUES (@Tipo, @Valor, @Descricao, @Categoria, @Data, @IdFuncionario, NULL);

    SET @IdMovimentacao = SCOPE_IDENTITY();
END;
GO

-- TESTE 17: Registrar movimentação manual
BEGIN TRANSACTION;

DECLARE @IdMovimentacao INT;

EXEC dbo.sp_RegistrarMovimentacao
    @Tipo = 'Saída',
    @Valor = 10.00,
    @Descricao = 'Movimentação de teste',
    @Categoria = 'Teste',
    @Data = NULL,
    @IdFuncionario = NULL,
    @IdMovimentacao = @IdMovimentacao OUTPUT;

SELECT *
FROM dbo.Movimentacao
WHERE IdMovimentacao = @IdMovimentacao;

ROLLBACK TRANSACTION;
GO

-- ---------------------------------------------------------------------
-- 18 -> Cards do fluxo de caixa. Sem datas: últimos 30 dias.
-- ---------------------------------------------------------------------
CREATE OR ALTER PROCEDURE dbo.sp_ResumoFluxoCaixa
    @DataInicial DATE = NULL,
    @DataFinal   DATE = NULL
AS
BEGIN
    SET NOCOUNT ON;

    SET @DataFinal   = ISNULL(@DataFinal, CAST(GETDATE() AS DATE));
    SET @DataInicial = ISNULL(@DataInicial, DATEADD(DAY, -30, CAST(GETDATE() AS DATE)));

    IF @DataInicial > @DataFinal
        THROW 50410, 'A data inicial não pode ser maior que a data final.', 1;

    DECLARE @Anterior DECIMAL(18,2) = dbo.fn_SaldoCaixaAte(DATEADD(DAY, -1, @DataInicial));
    DECLARE @Entradas DECIMAL(18,2), @Saidas DECIMAL(18,2);

    SELECT @Entradas = ISNULL(SUM(CASE WHEN Tipo = 'Entrada' THEN Valor END), 0),
           @Saidas   = ISNULL(SUM(CASE WHEN Tipo = 'Saída'   THEN Valor END), 0)
    FROM dbo.fn_MovimentacoesPorPeriodo(@DataInicial, @DataFinal);

    SELECT  @DataInicial                       AS DataInicial,
            @DataFinal                         AS DataFinal,
            @Anterior                          AS SaldoAnterior,
            @Entradas                          AS TotalEntradas,
            @Saidas                            AS TotalSaidas,
            @Anterior + @Entradas - @Saidas    AS SaldoAtual;
END;
GO

-- TESTE 18: Consultar resumo do fluxo de caixa
EXEC dbo.sp_ResumoFluxoCaixa;
GO

-- Teste para um período específico
EXEC dbo.sp_ResumoFluxoCaixa
    @DataInicial = '2026-01-01',
    @DataFinal = '2026-12-31';
GO

-- ---------------------------------------------------------------------
-- 19 -> -- Tabela do fluxo de caixa: período, tipo e ordenação.
-- @Ordenar: data | descricao | tipo | valor ; padrão: data crescente.
-- ---------------------------------------------------------------------

CREATE OR ALTER PROCEDURE dbo.sp_FluxoCaixa
    @DataInicial DATE        = NULL,
    @DataFinal   DATE        = NULL,
    @Tipo        VARCHAR(10) = NULL,
    @Ordenar     VARCHAR(20) = 'data',
    @Direcao     VARCHAR(4)  = 'asc'
AS
BEGIN
    SET NOCOUNT ON;

    SET @DataFinal   = ISNULL(@DataFinal, CAST(GETDATE() AS DATE));
    SET @DataInicial = ISNULL(@DataInicial, DATEADD(DAY, -30, CAST(GETDATE() AS DATE)));

    IF @DataInicial > @DataFinal
        THROW 50410, 'A data inicial não pode ser maior que a data final.', 1;

    IF @Direcao NOT IN ('asc', 'desc') SET @Direcao = 'asc';
    SET @Tipo = NULLIF(LTRIM(RTRIM(ISNULL(@Tipo, ''))), '');

    SELECT  f.IdMovimentacao, f.DataMovimentacao, f.Descricao, f.Tipo, f.Categoria, f.Valor,
            f.ValorComSinal, f.Origem, f.IdVenda, f.Funcionario
    FROM dbo.vw_FluxoCaixa f
    WHERE f.DataMovimentacao >= CAST(@DataInicial AS DATETIME)
      AND f.DataMovimentacao <  DATEADD(DAY, 1, CAST(@DataFinal AS DATETIME))
      AND (@Tipo IS NULL OR f.Tipo = @Tipo)
    ORDER BY
        CASE WHEN @Ordenar = 'descricao' AND @Direcao = 'asc'  THEN f.Descricao END ASC,
        CASE WHEN @Ordenar = 'descricao' AND @Direcao = 'desc' THEN f.Descricao END DESC,
        CASE WHEN @Ordenar = 'tipo'      AND @Direcao = 'asc'  THEN f.Tipo END ASC,
        CASE WHEN @Ordenar = 'tipo'      AND @Direcao = 'desc' THEN f.Tipo END DESC,
        CASE WHEN @Ordenar = 'valor'     AND @Direcao = 'asc'  THEN f.Valor END ASC,
        CASE WHEN @Ordenar = 'valor'     AND @Direcao = 'desc' THEN f.Valor END DESC,
        CASE WHEN @Ordenar NOT IN ('descricao', 'tipo', 'valor') AND @Direcao = 'asc'  THEN f.DataMovimentacao END ASC,
        CASE WHEN @Ordenar NOT IN ('descricao', 'tipo', 'valor') AND @Direcao = 'desc' THEN f.DataMovimentacao END DESC,
        f.IdMovimentacao ASC;
END;
GO

-- TESTE 19: Listar fluxo de caixa
EXEC dbo.sp_FluxoCaixa;
GO

-- Teste filtrando entradas e ordenando pelo valor
EXEC dbo.sp_FluxoCaixa
    @DataInicial = NULL,
    @DataFinal = NULL,
    @Tipo = 'Entrada',
    @Ordenar = 'valor',
    @Direcao = 'desc';
GO
-- ---------------------------------------------------------------------
-- 20 -> Dashboard
-- ---------------------------------------------------------------------
CREATE OR ALTER PROCEDURE dbo.sp_Dashboard
AS
BEGIN
    SET NOCOUNT ON;

    SELECT * FROM dbo.vw_Dashboard;

    -- últimos 30 dias
    SELECT Dia, QtdVendas, Faturamento
    FROM dbo.vw_VendasPorDia
    WHERE Dia >= DATEADD(DAY, -30, CAST(GETDATE() AS DATE))
    ORDER BY Dia;

    -- alertas de estoque
    SELECT IdProduto, Nome, Categoria, Estoque, EstoqueMinimo, DataValidade, DiasParaVencer,
           SemEstoque, EstoqueBaixo, Vencido, ValidadeProxima
    FROM dbo.vw_Estoque
    WHERE EmAlerta = 1
    ORDER BY Nome;

    -- últimas vendas
    SELECT TOP (5) IdVenda, DataVenda, Funcionario, QtdItens, ValorTotal, Status
    FROM dbo.vw_Vendas
    ORDER BY DataVenda DESC;
END;
GO

-- TESTE 20: Consultar dados do dashboard
EXEC dbo.sp_Dashboard;
GO

/* =====================================================================
   INSERTS - ItsFoodBrasil
   Rode depois do script do banco, uma vez, na ordem.
 
   Tabela          Como é preenchida
   Pessoa          sp_CadastrarFuncionario (3 pessoas)
   Funcionario     sp_CadastrarFuncionario (3 funcionários)
   Categoria       sp_CadastrarCategoria   (3 categorias)
   Produto         sp_CadastrarProduto     (6 produtos)
   Venda           sp_RegistrarVenda       (3 vendas)
   ItemVenda       sp_RegistrarVenda       (7 itens)
   Movimentacao    sp_RegistrarVenda (3 entradas) + sp_RegistrarMovimentacao (3 lançamentos)
   ===================================================================== */
 
 
/* ---------------------------------------------------------------------
   1. Pessoa + Funcionario
--------------------------------------------------------------------- */
declare @id int;
 
if not exists (select 1 from dbo.Pessoa where Email = 'marcos@itsfoodbrasil.com.br')
    exec dbo.sp_CadastrarFuncionario
         @Nome = 'Marcos Oliveira', @Email = 'marcos@itsfoodbrasil.com.br',
         @Perfil = 'Funcionario', @IdPessoa = @id output;
 
if not exists (select 1 from dbo.Pessoa where Email = 'juliana@itsfoodbrasil.com.br')
    exec dbo.sp_CadastrarFuncionario
         @Nome = 'Juliana Rocha', @Email = 'juliana@itsfoodbrasil.com.br',
         @Perfil = 'Funcionario', @IdPessoa = @id output;
 
if not exists (select 1 from dbo.Pessoa where Email = 'roberto@itsfoodbrasil.com.br')
    exec dbo.sp_CadastrarFuncionario
         @Nome = 'Roberto Almeida', @Email = 'roberto@itsfoodbrasil.com.br',
         @Perfil = 'Admin', @IdPessoa = @id output;
go
 
 
/* ---------------------------------------------------------------------
   2. Categoria
--------------------------------------------------------------------- */
declare @id int;
 
if not exists (select 1 from dbo.Categoria where Nome = 'Congelados')
    exec dbo.sp_CadastrarCategoria
         @Nome = 'Congelados', @Descricao = 'Hambúrgueres, nuggets e empanados',
         @IdCategoria = @id output;
 
if not exists (select 1 from dbo.Categoria where Nome = 'Carnes Suínas')
    exec dbo.sp_CadastrarCategoria
         @Nome = 'Carnes Suínas', @Descricao = 'Costelinhas, copas e lombos',
         @IdCategoria = @id output;
 
if not exists (select 1 from dbo.Categoria where Nome = 'Temperos e Molhos')
    exec dbo.sp_CadastrarCategoria
         @Nome = 'Temperos e Molhos', @Descricao = 'Molhos prontos e temperos para embutidos',
         @IdCategoria = @id output;
go
 
 
/* ---------------------------------------------------------------------
   3. Produto
--------------------------------------------------------------------- */
declare @id int,
        @congelados int = (select IdCategoria from Categoria where Nome = 'Congelados'),
        @suinas     int = (select IdCategoria from Categoria where Nome = 'Carnes Suínas'),
        @temperos   int = (select IdCategoria from Categoria where Nome = 'Temperos e Molhos');
 
if not exists (select 1 from dbo.Produto where Nome = 'Hambúrguer bovino 672g')
    exec dbo.sp_CadastrarProduto
         @IdCategoria = @congelados, @Nome = 'Hambúrguer bovino 672g',
     @Descricao = 'Caixa com 12 unidades', @Preco = 18.90,
     @QuantidadeEstoque = 150, @EstoqueMinimo = 30, @DataValidade = '2027-03-15',
     @IdProduto = @id output;
 
if not exists (select 1 from dbo.Produto where Nome = 'Nuggets de frango 300g')
    exec dbo.sp_CadastrarProduto
         @IdCategoria = @congelados, @Nome = 'Nuggets de frango 300g',
     @Descricao = 'Pacote congelado', @Preco = 16.50,
     @QuantidadeEstoque = 100, @EstoqueMinimo = 25, @DataValidade = '2027-02-20',
     @IdProduto = @id output;
 
if not exists (select 1 from dbo.Produto where Nome = 'Costelinha suína defumada 1kg')
    exec dbo.sp_CadastrarProduto
         @IdCategoria = @suinas, @Nome = 'Costelinha suína defumada 1kg',
     @Descricao = 'Resfriada, embalada a vácuo', @Preco = 42.00,
     @QuantidadeEstoque = 60, @EstoqueMinimo = 15, @DataValidade = '2026-12-30',
     @IdProduto = @id output;
 
if not exists (select 1 from dbo.Produto where Nome = 'Copa lombo fatiada 200g')
    exec dbo.sp_CadastrarProduto
         @IdCategoria = @suinas, @Nome = 'Copa lombo fatiada 200g',
     @Descricao = 'Fatiada, embalada a vácuo', @Preco = 19.90,
     @QuantidadeEstoque = 80, @EstoqueMinimo = 20, @DataValidade = '2027-01-25',
     @IdProduto = @id output;
 
if not exists (select 1 from dbo.Produto where Nome = 'Molho barbecue 400g')
    exec dbo.sp_CadastrarProduto
         @IdCategoria = @temperos, @Nome = 'Molho barbecue 400g',
     @Descricao = 'Frasco de 400g', @Preco = 11.50,
     @QuantidadeEstoque = 90, @EstoqueMinimo = 20, @DataValidade = '2027-08-10',
     @IdProduto = @id output;
 
if not exists (select 1 from dbo.Produto where Nome = 'Tempero para linguiça 100g')
    exec dbo.sp_CadastrarProduto
         @IdCategoria = @temperos, @Nome = 'Tempero para linguiça 100g',
     @Descricao = 'Sachê de 100g', @Preco = 6.90,
     @QuantidadeEstoque = 120, @EstoqueMinimo = 30, @DataValidade = '2027-09-05',
     @IdProduto = @id output;
go
 
 
/* ---------------------------------------------------------------------
   4. Venda + ItemVenda + Movimentacao (entrada gerada pela venda)
   A procedure confere o estoque, baixa as quantidades e lança no caixa.
--------------------------------------------------------------------- */
declare @marcos  int = (select IdPessoa from Pessoa where Email = 'marcos@itsfoodbrasil.com.br'),
        @juliana int = (select IdPessoa from Pessoa where Email = 'juliana@itsfoodbrasil.com.br'),
        @roberto int = (select IdPessoa from Pessoa where Email = 'roberto@itsfoodbrasil.com.br'),
        @itens TipoItemVenda,
        @venda int;
 
-- venda 1 (Marcos): 2 itens, sem desconto
if not exists (select 1 from dbo.Venda v join dbo.ItemVenda i on i.IdVenda = v.IdVenda join dbo.Produto p on p.IdProduto = i.IdProduto where v.IdFuncionario = @marcos and p.Nome = 'Hambúrguer bovino 672g' and i.Quantidade = 20)
begin
    insert @itens values
        ((select IdProduto from dbo.Produto where Nome = 'Hambúrguer bovino 672g'), 20),
        ((select IdProduto from dbo.Produto where Nome = 'Molho barbecue 400g'), 10);
    exec dbo.sp_RegistrarVenda @IdFuncionario = @marcos, @Itens = @itens, @Desconto = 0, @IdVenda = @venda output;
end;
 
-- venda 2 (Juliana): 2 itens, desconto de 10,00
delete @itens;
if not exists (select 1 from dbo.Venda v join dbo.ItemVenda i on i.IdVenda = v.IdVenda join dbo.Produto p on p.IdProduto = i.IdProduto where v.IdFuncionario = @juliana and p.Nome = 'Costelinha suína defumada 1kg' and i.Quantidade = 8)
begin
    insert @itens values
        ((select IdProduto from dbo.Produto where Nome = 'Costelinha suína defumada 1kg'), 8),
        ((select IdProduto from dbo.Produto where Nome = 'Nuggets de frango 300g'), 15);
    exec dbo.sp_RegistrarVenda @IdFuncionario = @juliana, @Itens = @itens, @Desconto = 10, @IdVenda = @venda output;
end;
 
-- venda 3 (Roberto): 3 itens, sem desconto
delete @itens;
if not exists (select 1 from dbo.Venda v join dbo.ItemVenda i on i.IdVenda = v.IdVenda join dbo.Produto p on p.IdProduto = i.IdProduto where v.IdFuncionario = @roberto and p.Nome = 'Copa lombo fatiada 200g' and i.Quantidade = 12)
begin
    insert @itens values
        ((select IdProduto from dbo.Produto where Nome = 'Copa lombo fatiada 200g'), 12),
        ((select IdProduto from dbo.Produto where Nome = 'Tempero para linguiça 100g'), 25),
        ((select IdProduto from dbo.Produto where Nome = 'Hambúrguer bovino 672g'), 5);
    exec dbo.sp_RegistrarVenda @IdFuncionario = @roberto, @Itens = @itens, @Desconto = 0, @IdVenda = @venda output;
end;
go
 
 
/* ---------------------------------------------------------------------
   5. Movimentacao (lançamentos manuais do fluxo de caixa)
--------------------------------------------------------------------- */
declare @roberto int = (select IdPessoa from Pessoa where Email = 'roberto@itsfoodbrasil.com.br'),
        @id int;
 
if not exists (select 1 from dbo.Movimentacao where Descricao = 'Compra de embalagens a vácuo' and IdVenda is null)
    exec dbo.sp_RegistrarMovimentacao
         @Tipo = 'Saída', @Valor = 2750.00, @Descricao = 'Compra de embalagens a vácuo',
         @Categoria = 'Compras', @IdFuncionario = @roberto, @IdMovimentacao = @id output;
 
if not exists (select 1 from dbo.Movimentacao where Descricao = 'Manutenção da câmara fria' and IdVenda is null)
    exec dbo.sp_RegistrarMovimentacao
         @Tipo = 'Saída', @Valor = 980.00, @Descricao = 'Manutenção da câmara fria',
         @Categoria = 'Despesas', @IdFuncionario = @roberto, @IdMovimentacao = @id output;
 
if not exists (select 1 from dbo.Movimentacao where Descricao = 'Suprimento de caixa' and IdVenda is null)
    exec dbo.sp_RegistrarMovimentacao
         @Tipo = 'Entrada', @Valor = 1500.00, @Descricao = 'Suprimento de caixa',
         @Categoria = 'Suprimento de caixa', @IdFuncionario = @roberto, @IdMovimentacao = @id output;
go


select * from Pessoa
select * from Movimentacao
select * from Venda
select * from ItemVenda
select * from Produto
