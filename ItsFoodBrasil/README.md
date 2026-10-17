# Its Food Brasil

Requisitos: .NET SDK (o projeto usa `net10.0`) e SQL Server (Express serve).

## 1. Banco de dados

Na pasta `Banco`, execute no SSMS, nesta ordem:

1. `01_ItsFoodBrasil_Banco_SQLServer_Atualizado.sql` (cria o banco `ItsFoodBrasil`, tabelas, views, procedures e dados de exemplo)
2. `02_Ajustes_Integracao.sql` (senha em hash e categoria nas movimentações)

## 2. Connection string

Em `ItsFoodBrasil/appsettings.json`, ajuste o `Server` para o seu SQL Server
(por exemplo `localhost`, `.\SQLEXPRESS` ou `(localdb)\MSSQLLocalDB`).

## 3. Executar

    cd ItsFoodBrasil
    dotnet run

Abre em http://localhost:5080 (tela de login).

## Login

Os usuários de exemplo do script SQL recebem a senha `Senha@123` na primeira execução:

| E-mail                        | Perfil        |
|-------------------------------|---------------|
| admin@itsfoodbrasil.com.br    | Administrador |
| carlos@itsfoodbrasil.com.br   | Funcionário   |
| ana@itsfoodbrasil.com.br      | Funcionário   |

Troque as senhas em Cadastros > Editar. Novos funcionários já são gravados com senha em hash.

## Como o sistema acessa o banco

- `Data/Db.cs`: abre a conexão e executa consultas e procedures (ADO.NET).
- `Data/*Store.cs`: uma classe por assunto (Cadastro, Produto, Venda, Movimentacao). Os controllers só chamam esses métodos.
- Gravações usam as procedures do banco (`sp_RegistrarVenda`, `sp_CadastrarProduto`, ...); as mensagens de erro de regra de negócio (ex.: estoque insuficiente) aparecem na tela.
