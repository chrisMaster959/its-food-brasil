/* =====================================================================
   ItsFoodBrasil - Distribuição de Embutidos
   Script completo para SQL Server (2016 SP1 ou superior)
   Gerado a partir do DER: Pessoa, Funcionario, Categoria, Produto,
   Venda, ItemVenda, Movimentacao.

   Ordem do script:
     1. Banco de dados
     2. Tabelas, constraints e índices
     3. Tipo de tabela (TVP) usado na venda
     4. Functions
     5. Views
     6. Procedures
     7. Dados de exemplo (só se o banco estiver vazio)

   O script pode ser executado mais de uma vez (idempotente).
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
-- ---------------------------------------------------------------------
IF OBJECT_ID(N'dbo.Pessoa', N'U') IS NULL
CREATE TABLE dbo.Pessoa
(
    IdPessoa        INT IDENTITY(1,1) NOT NULL,
    Nome            VARCHAR(150)      NOT NULL,
    CPF             VARCHAR(14)       NULL,
    Email           VARCHAR(150)      NULL,
    Senha           VARCHAR(50)       NULL,
    Telefone        VARCHAR(20)       NULL,
    DataNascimento  DATE              NULL,

    CONSTRAINT PK_Pessoa PRIMARY KEY (IdPessoa),
    CONSTRAINT CK_Pessoa_Nome CHECK (LEN(LTRIM(RTRIM(Nome))) > 0),
    CONSTRAINT CK_Pessoa_DataNascimento CHECK (DataNascimento IS NULL OR DataNascimento <= CAST(GETDATE() AS DATE))
);
GO

-- CPF e e-mail são únicos quando informados
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UX_Pessoa_CPF' AND object_id = OBJECT_ID(N'dbo.Pessoa'))
    CREATE UNIQUE INDEX UX_Pessoa_CPF   ON dbo.Pessoa (CPF)   WHERE CPF   IS NOT NULL;
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
-- Cada venda gera no máximo uma movimentação (índice único filtrado).
-- ---------------------------------------------------------------------
IF OBJECT_ID(N'dbo.Movimentacao', N'U') IS NULL
CREATE TABLE dbo.Movimentacao
(
    IdMovimentacao    INT IDENTITY(1,1) NOT NULL,
    Tipo              VARCHAR(10)       NOT NULL,
    Valor             DECIMAL(18,2)     NOT NULL,
    Descricao         VARCHAR(200)      NOT NULL,
    Categoria         VARCHAR(50)           NULL,
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

-- Valida CPF (aceita com ou sem pontuação).
CREATE OR ALTER FUNCTION dbo.fn_CpfValido(@Cpf VARCHAR(14))
RETURNS BIT
AS
BEGIN
    DECLARE @n VARCHAR(11) = REPLACE(REPLACE(REPLACE(ISNULL(@Cpf, ''), '.', ''), '-', ''), ' ', '');
    DECLARE @i INT, @soma INT, @d1 INT, @d2 INT;

    IF LEN(@n) <> 11 OR @n LIKE '%[^0-9]%'
        RETURN 0;

    -- sequências como 111.111.111-11 são inválidas
    IF REPLACE(@n, LEFT(@n, 1), '') = ''
        RETURN 0;

    SET @i = 1; SET @soma = 0;
    WHILE @i <= 9
    BEGIN
        SET @soma += CAST(SUBSTRING(@n, @i, 1) AS INT) * (11 - @i);
        SET @i += 1;
    END
    SET @d1 = (@soma * 10) % 11;
    IF @d1 = 10 SET @d1 = 0;
    IF @d1 <> CAST(SUBSTRING(@n, 10, 1) AS INT)
        RETURN 0;

    SET @i = 1; SET @soma = 0;
    WHILE @i <= 10
    BEGIN
        SET @soma += CAST(SUBSTRING(@n, @i, 1) AS INT) * (12 - @i);
        SET @i += 1;
    END
    SET @d2 = (@soma * 10) % 11;
    IF @d2 = 10 SET @d2 = 0;
    IF @d2 <> CAST(SUBSTRING(@n, 11, 1) AS INT)
        RETURN 0;

    RETURN 1;
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

-- Funcionários com os dados da pessoa (tela de Cadastros).
CREATE OR ALTER VIEW dbo.vw_Funcionarios
AS
SELECT  f.IdPessoa AS IdFuncionario,
        p.Nome,
        p.CPF,
        p.Email,
        p.Telefone,
        p.DataNascimento,
        f.Perfil,
        f.Ativo
FROM dbo.Funcionario f
JOIN dbo.Pessoa      p ON p.IdPessoa = f.IdPessoa;
GO

-- Estoque com os alertas (sem estoque, baixo, vencido, validade próxima).
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

-- Lista de vendas (tela Vendas).
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

-- Itens vendidos (tela de detalhes da venda).
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

-- Movimentações do fluxo de caixa, com a origem (venda ou lançamento manual).
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
        pe.Nome AS Funcionario
FROM dbo.Movimentacao m
LEFT JOIN dbo.Pessoa  pe ON pe.IdPessoa = m.IdFuncionario;
GO

-- Produtos mais vendidos (somente vendas concluídas).
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

-- Vendas concluídas por dia (gráfico dos últimos 30 dias do dashboard).
CREATE OR ALTER VIEW dbo.vw_VendasPorDia
AS
SELECT  CAST(v.DataVenda AS DATE) AS Dia,
        COUNT(*)                  AS QtdVendas,
        SUM(v.ValorTotal)         AS Faturamento
FROM dbo.Venda v
WHERE v.Status = 'Concluída'
GROUP BY CAST(v.DataVenda AS DATE);
GO

-- Cards do dashboard (uma linha).
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


/* =====================================================================
   6. PROCEDURES
   Erros de regra de negócio usam THROW (50000+) com mensagem em português.
   ===================================================================== */

-- ---------------------------------------------------------------------
-- Funcionários
-- ---------------------------------------------------------------------
CREATE OR ALTER PROCEDURE dbo.sp_CadastrarFuncionario
    @Nome            VARCHAR(150),
    @CPF             VARCHAR(14)  = NULL,
    @Email           VARCHAR(150) = NULL,
    @Telefone        VARCHAR(20)  = NULL,
    @DataNascimento  DATE         = NULL,
    @Perfil          VARCHAR(20)  = 'Funcionario',
    @IdPessoa        INT          OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    SET @Nome  = LTRIM(RTRIM(ISNULL(@Nome, '')));
    SET @Email = NULLIF(LTRIM(RTRIM(ISNULL(@Email, ''))), '');
    SET @CPF   = NULLIF(REPLACE(REPLACE(REPLACE(ISNULL(@CPF, ''), '.', ''), '-', ''), ' ', ''), '');

    IF @Nome = ''
        THROW 50100, 'Informe o nome do funcionário.', 1;

    IF @Perfil NOT IN ('Admin', 'Funcionario')
        THROW 50101, 'Perfil inválido. Use Admin ou Funcionario.', 1;

    IF @CPF IS NOT NULL AND dbo.fn_CpfValido(@CPF) = 0
        THROW 50102, 'CPF inválido.', 1;

    IF @CPF IS NOT NULL AND EXISTS (SELECT 1 FROM dbo.Pessoa WHERE CPF = @CPF)
        THROW 50103, 'Já existe uma pessoa cadastrada com esse CPF.', 1;

    IF @Email IS NOT NULL AND EXISTS (SELECT 1 FROM dbo.Pessoa WHERE Email = @Email)
        THROW 50104, 'Já existe uma pessoa cadastrada com esse e-mail.', 1;

    BEGIN TRY
        BEGIN TRAN;

        INSERT dbo.Pessoa (Nome, CPF, Email, Telefone, DataNascimento)
        VALUES (@Nome, @CPF, @Email, @Telefone, @DataNascimento);

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

CREATE OR ALTER PROCEDURE dbo.sp_AtualizarFuncionario
    @IdPessoa        INT,
    @Nome            VARCHAR(150),
    @CPF             VARCHAR(14)  = NULL,
    @Email           VARCHAR(150) = NULL,
    @Telefone        VARCHAR(20)  = NULL,
    @DataNascimento  DATE         = NULL,
    @Perfil          VARCHAR(20)
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    SET @Nome  = LTRIM(RTRIM(ISNULL(@Nome, '')));
    SET @Email = NULLIF(LTRIM(RTRIM(ISNULL(@Email, ''))), '');
    SET @CPF   = NULLIF(REPLACE(REPLACE(REPLACE(ISNULL(@CPF, ''), '.', ''), '-', ''), ' ', ''), '');

    IF NOT EXISTS (SELECT 1 FROM dbo.Funcionario WHERE IdPessoa = @IdPessoa)
        THROW 50110, 'Funcionário não encontrado.', 1;

    IF @Nome = ''
        THROW 50100, 'Informe o nome do funcionário.', 1;

    IF @Perfil NOT IN ('Admin', 'Funcionario')
        THROW 50101, 'Perfil inválido. Use Admin ou Funcionario.', 1;

    IF @CPF IS NOT NULL AND dbo.fn_CpfValido(@CPF) = 0
        THROW 50102, 'CPF inválido.', 1;

    IF @CPF IS NOT NULL AND EXISTS (SELECT 1 FROM dbo.Pessoa WHERE CPF = @CPF AND IdPessoa <> @IdPessoa)
        THROW 50103, 'Já existe uma pessoa cadastrada com esse CPF.', 1;

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
           SET Nome = @Nome, CPF = @CPF, Email = @Email,
               Telefone = @Telefone, DataNascimento = @DataNascimento
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

-- Botão do "olho": deixa de exibir / reativa.
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

-- Excluir: só se o funcionário nunca registrou venda nem movimentação.
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

-- Lista de cadastros (busca + situação + ordenação).
-- @Ordenar: nome | perfil | status ; @Direcao: asc | desc
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
    WHERE (@Busca IS NULL OR Nome LIKE '%' + @Busca + '%'
                          OR CPF  LIKE '%' + @Busca + '%'
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

-- ---------------------------------------------------------------------
-- Categorias e produtos
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

-- Botão do "olho" do estoque: desativado deixa de aparecer no caixa.
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

-- Produto já vendido não pode ser excluído (só desativado).
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

-- Estoque: busca, categoria, situação e ordenação.
-- @Situacao: baixo | sem | proxima | vencido | inativo
-- @Ordenar : produto | categoria | preco | estoque | validade | status
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

-- Indicadores do topo da tela de estoque (sobre os produtos ativos).
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

-- ---------------------------------------------------------------------
-- Vendas (Caixa / PDV)
-- Preços vêm SEMPRE do banco. Tudo em uma transação:
-- valida, grava venda e itens, baixa o estoque e gera a entrada no caixa.
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

-- Cancelar: devolve o estoque e remove a entrada que a venda gerou no caixa.
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

-- Tela Vendas: filtros (datas inclusivas, status) + ordenação.
-- @Ordenar: venda | data | funcionario | itens | desconto | valor | status
-- Padrão: data decrescente. Devolve também o total das vendas concluídas listadas.
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

-- Detalhes da venda: cabeçalho + itens.
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

-- ---------------------------------------------------------------------
-- Fluxo de caixa
-- ---------------------------------------------------------------------

-- Lançamento manual ("Nova movimentação").
CREATE OR ALTER PROCEDURE dbo.sp_RegistrarMovimentacao
    @Tipo            VARCHAR(10),
    @Valor           DECIMAL(18,2),
    @Descricao       VARCHAR(200),
    @Data            DATETIME = NULL,
    @IdFuncionario   INT      = NULL,
    @IdMovimentacao  INT      OUTPUT
AS
BEGIN
    SET NOCOUNT ON;

    SET @Descricao = LTRIM(RTRIM(ISNULL(@Descricao, '')));
    SET @Data = ISNULL(@Data, GETDATE());

    IF @Tipo NOT IN ('Entrada', 'Saída')
        THROW 50400, 'Tipo inválido. Use Entrada ou Saída.', 1;
    IF @Descricao = ''
        THROW 50401, 'Informe a descrição.', 1;
    IF @Valor IS NULL OR @Valor <= 0
        THROW 50402, 'Informe um valor maior que zero.', 1;
    IF @IdFuncionario IS NOT NULL AND NOT EXISTS (SELECT 1 FROM dbo.Funcionario WHERE IdPessoa = @IdFuncionario)
        THROW 50403, 'Funcionário não encontrado.', 1;

    INSERT dbo.Movimentacao (Tipo, Valor, Descricao, DataMovimentacao, IdFuncionario, IdVenda)
    VALUES (@Tipo, @Valor, @Descricao, @Data, @IdFuncionario, NULL);

    SET @IdMovimentacao = SCOPE_IDENTITY();
END;
GO

-- Cards do fluxo de caixa. Sem datas: últimos 30 dias.
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

-- Tabela do fluxo de caixa: período, tipo e ordenação.
-- @Ordenar: data | descricao | tipo | valor ; padrão: data crescente.
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

    SELECT  f.IdMovimentacao, f.DataMovimentacao, f.Descricao, f.Tipo, f.Valor,
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

-- ---------------------------------------------------------------------
-- Dashboard
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


/* =====================================================================
   7. DADOS DE EXEMPLO (só entram se as tabelas estiverem vazias)
   ===================================================================== */

IF NOT EXISTS (SELECT 1 FROM dbo.Categoria)
BEGIN
    INSERT dbo.Categoria (Nome, Descricao) VALUES
        ('Embutidos', 'Salsichas, linguiças, mortadelas e apresuntados'),
        ('Frios',     'Presuntos, salames e peito de peru'),
        ('Defumados', 'Bacon e produtos defumados'),
        ('Laticínios','Queijos');
END
GO

IF NOT EXISTS (SELECT 1 FROM dbo.Produto)
BEGIN
    INSERT dbo.Produto (IdCategoria, Nome, Preco, QuantidadeEstoque, EstoqueMinimo, DataValidade)
    SELECT c.IdCategoria, v.Nome, v.Preco, v.Qtd, 20, DATEADD(DAY, v.Dias, CAST(GETDATE() AS DATE))
    FROM (VALUES
        ('Embutidos',  'Salsicha hot dog 500g',       9.80, 120, 90),
        ('Embutidos',  'Linguiça calabresa 500g',    14.90,  80, 60),
        ('Embutidos',  'Linguiça toscana 1kg',       21.90,  60, 45),
        ('Embutidos',  'Mortadela defumada 1kg',     24.50,  50, 70),
        ('Embutidos',  'Apresuntado 1kg',            17.50,  15, 40),
        ('Frios',      'Presunto fatiado 200g',      12.90, 100, 30),
        ('Frios',      'Peito de peru fatiado 200g', 15.90,  70, 25),
        ('Frios',      'Salame italiano 300g',       32.00,  40, 120),
        ('Defumados',  'Bacon em manta 1kg',         38.90,  35, 50),
        ('Laticínios', 'Queijo mussarela 500g',      27.00,  90, 12)
    ) AS v (Categoria, Nome, Preco, Qtd, Dias)
    JOIN dbo.Categoria c ON c.Nome = v.Categoria;
END
GO

IF NOT EXISTS (SELECT 1 FROM dbo.Pessoa)
BEGIN
    DECLARE @idAdmin INT, @idFunc INT;

    -- CPFs de exemplo (válidos pelo algoritmo, apenas para teste)
    EXEC dbo.sp_CadastrarFuncionario
         @Nome = 'Administrador', @CPF = '529.982.247-25', @Email = 'admin@itsfoodbrasil.com.br',
         @Telefone = '(19) 99999-0001', @DataNascimento = '1985-03-10', @Perfil = 'Admin',
         @IdPessoa = @idAdmin OUTPUT;

    EXEC dbo.sp_CadastrarFuncionario
         @Nome = 'Carlos Silva', @CPF = '168.995.350-09', @Email = 'carlos@itsfoodbrasil.com.br',
         @Telefone = '(19) 99999-0002', @DataNascimento = '1992-07-21', @Perfil = 'Funcionario',
         @IdPessoa = @idFunc OUTPUT;

    EXEC dbo.sp_CadastrarFuncionario
         @Nome = 'Ana Souza', @CPF = '111.444.777-35', @Email = 'ana@itsfoodbrasil.com.br',
         @Telefone = '(19) 99999-0003', @DataNascimento = '1995-11-02', @Perfil = 'Funcionario',
         @IdPessoa = @idFunc OUTPUT;
END
GO

IF NOT EXISTS (SELECT 1 FROM dbo.Movimentacao)
BEGIN
    DECLARE @admin INT = (SELECT TOP 1 IdPessoa FROM dbo.Funcionario WHERE Perfil = 'Admin' ORDER BY IdPessoa);

    INSERT dbo.Movimentacao (Tipo, Valor, Descricao, DataMovimentacao, IdFuncionario) VALUES
        ('Saída',   6500.00, 'Pagamento de aluguel',          DATEADD(DAY, -28, GETDATE()), @admin),
        ('Saída',  10800.00, 'Pagamento fornecedor Frimesa',  DATEADD(DAY, -24, GETDATE()), @admin),
        ('Saída',   4230.00, 'Pagamento de energia',          DATEADD(DAY, -20, GETDATE()), @admin),
        ('Saída',   9500.00, 'Compra de mercadorias',         DATEADD(DAY, -15, GETDATE()), @admin),
        ('Entrada', 3000.00, 'Suprimento de caixa',           DATEADD(DAY, -12, GETDATE()), @admin),
        ('Saída',   8800.00, 'Compra de mercadorias',         DATEADD(DAY,  -9, GETDATE()), @admin),
        ('Saída',   1250.00, 'Pagamento de frete',            DATEADD(DAY,  -5, GETDATE()), @admin),
        ('Saída',  12450.00, 'Pagamento fornecedor Frimesa',  DATEADD(DAY,  -2, GETDATE()), @admin);
END
GO

-- Duas vendas de exemplo, feitas pela própria procedure (baixa estoque e gera movimentação)
IF NOT EXISTS (SELECT 1 FROM dbo.Venda)
BEGIN
    DECLARE @func INT = (SELECT TOP 1 IdPessoa FROM dbo.Pessoa WHERE Nome = 'Carlos Silva');
    DECLARE @itens dbo.TipoItemVenda, @nova INT;

    INSERT @itens VALUES
        ((SELECT IdProduto FROM dbo.Produto WHERE Nome = 'Presunto fatiado 200g'), 20),
        ((SELECT IdProduto FROM dbo.Produto WHERE Nome = 'Mortadela defumada 1kg'), 10),
        ((SELECT IdProduto FROM dbo.Produto WHERE Nome = 'Salsicha hot dog 500g'), 30);
    EXEC dbo.sp_RegistrarVenda @IdFuncionario = @func, @Itens = @itens, @Desconto = 0, @IdVenda = @nova OUTPUT;

    DELETE @itens;
    INSERT @itens VALUES
        ((SELECT IdProduto FROM dbo.Produto WHERE Nome = 'Linguiça toscana 1kg'), 10),
        ((SELECT IdProduto FROM dbo.Produto WHERE Nome = 'Bacon em manta 1kg'), 5);
    EXEC dbo.sp_RegistrarVenda @IdFuncionario = @func, @Itens = @itens, @Desconto = 5, @IdVenda = @nova OUTPUT;
END
GO


/* =====================================================================
   EXEMPLOS DE USO
   =====================================================================

   -- Fluxo de caixa dos últimos 30 dias, só saídas, maior valor primeiro
   EXEC dbo.sp_ResumoFluxoCaixa;
   EXEC dbo.sp_FluxoCaixa @Tipo = 'Saída', @Ordenar = 'valor', @Direcao = 'desc';

   -- Vendas concluídas do mês, por funcionário
   DECLARE @total DECIMAL(18,2);
   EXEC dbo.sp_ListarVendas @DataInicial = '2026-10-01', @DataFinal = '2026-10-31',
                            @Status = 'Concluída', @Ordenar = 'funcionario', @Direcao = 'asc',
                            @TotalConcluidas = @total OUTPUT;
   SELECT @total AS TotalConcluidas;

   -- Estoque com validade próxima, ordenado por validade
   EXEC dbo.sp_ListarEstoque @Situacao = 'proxima', @Ordenar = 'validade';

   -- Nova venda
   DECLARE @itens dbo.TipoItemVenda, @id INT;
   INSERT @itens VALUES (1, 2), (3, 1);
   EXEC dbo.sp_RegistrarVenda @IdFuncionario = 2, @Itens = @itens, @Desconto = 0, @IdVenda = @id OUTPUT;
   SELECT @id AS NovaVenda;

   ===================================================================== */

   /* =====================================================================
   ItsFoodBrasil - AJUSTES PARA A INTEGRAÇÃO COM O SISTEMA (.NET)

   Execute DEPOIS do script ItsFoodBrasil_Banco_SQLServer_Atualizado.sql.
   Pode ser executado mais de uma vez (idempotente).

   O que este script faz:
     1. Pessoa.Senha passa de VARCHAR(50) para VARCHAR(255)
        (a senha é gravada como HASH, que é maior que 50 caracteres).
     2. Movimentacao ganha a coluna Categoria
        (a tela de Fluxo de Caixa filtra e exibe a categoria do lançamento).
     3. vw_FluxoCaixa passa a devolver a Categoria
        (vendas sem categoria aparecem como 'Vendas').
     4. sp_RegistrarMovimentacao passa a receber @Categoria.
   ===================================================================== */

USE ItsFoodBrasil;
GO

SET NOCOUNT ON;
GO

-- ---------------------------------------------------------------------
-- 1. Senha em hash
-- ---------------------------------------------------------------------
ALTER TABLE dbo.Pessoa ALTER COLUMN Senha VARCHAR(255) NULL;
GO

-- ---------------------------------------------------------------------
-- 2. Categoria da movimentação
-- ---------------------------------------------------------------------
IF COL_LENGTH(N'dbo.Movimentacao', N'Categoria') IS NULL
    ALTER TABLE dbo.Movimentacao ADD Categoria VARCHAR(50) NULL;
GO

-- Lançamentos manuais antigos (dados de exemplo) ficam com uma categoria coerente.
-- As entradas geradas por venda continuam sem categoria: a view mostra 'Vendas'.
UPDATE dbo.Movimentacao
   SET Categoria = CASE
        WHEN Descricao LIKE 'Pagamento de aluguel%'   THEN 'Despesas'
        WHEN Descricao LIKE 'Pagamento de energia%'   THEN 'Despesas'
        WHEN Descricao LIKE 'Pagamento fornecedor%'   THEN 'Fornecedores'
        WHEN Descricao LIKE 'Compra de mercadorias%'  THEN 'Compras'
        WHEN Descricao LIKE 'Pagamento de frete%'     THEN 'Frete'
        WHEN Descricao LIKE 'Suprimento de caixa%'    THEN 'Suprimento de caixa'
        WHEN Tipo = 'Entrada'                         THEN 'Outras entradas'
        ELSE 'Outras saídas'
   END
 WHERE Categoria IS NULL
   AND IdVenda IS NULL;
GO

-- ---------------------------------------------------------------------
-- 3. View do fluxo de caixa com a categoria
-- ---------------------------------------------------------------------
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

-- ---------------------------------------------------------------------
-- 4. Lançamento manual com categoria
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