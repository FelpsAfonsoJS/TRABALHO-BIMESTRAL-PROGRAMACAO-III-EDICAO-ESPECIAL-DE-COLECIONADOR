<div align="center">

![HTML5](https://img.shields.io/badge/HTML5-estrutura-E34F26?style=for-the-badge&logo=html5&logoColor=white)
![CSS3](https://img.shields.io/badge/CSS3-interface-1572B6?style=for-the-badge&logo=css3&logoColor=white)
![JavaScript](https://img.shields.io/badge/JavaScript-interatividade-F7DF1E?style=for-the-badge&logo=javascript&logoColor=111111)
![Projeto acadêmico](https://img.shields.io/badge/UMFG-Programação%20III-56634C?style=for-the-badge)

# 🎯 Lembretes da Missão

### Planeje a rodada antes que a bomba exploda.

Um gerenciador de lembretes acadêmico com uma interface inspirada na atmosfera tática e na estética clássica de **Counter-Strike 1.6**.

**STATUS DA RODADA:** `PROJETO ACADÊMICO` &nbsp;·&nbsp; **MISSÃO:** `ORGANIZAR LEMBRETES`

</div>

---

## 🧭 Sobre o projeto

O **Lembretes da Missão** é um front-end para cadastrar e organizar lembretes. O tema visual faz referência ao clima de Counter-Strike 1.6: paleta tática, elementos de HUD e linguagem de missão. A aplicação se conecta a uma API em ASP.NET Core e usa um banco MySQL.

## ✨ Recursos

- 📝 Criar, editar e excluir lembretes.
- 🔎 Filtrar por texto, data de cadastro e data de atualização.
- ↕️ Ordenar os resultados por descrição, datas ou status.
- 📄 Navegar pelas páginas e escolher quantos registros exibir.
- 🖨️ Gerar e baixar um relatório em PDF.
- 📱 Interface adaptável, com estados de carregamento, mensagens e confirmação de exclusão.

## 🧰 Tecnologias

| Camada | Tecnologias |
|---|---|
| Front-end | HTML, CSS e JavaScript |
| Back-end | ASP.NET Core Minimal API, .NET 8 e Entity Framework Core |
| Persistência | MySQL |
| Relatórios | QuestPDF |

## 🚦 Como executar

O front-end e a API são projetos separados. Para usar cadastro, filtros e demais operações, inicie o MySQL e a API antes de abrir a página.

### 1. Banco de dados e API

É necessário ter o **.NET 8 SDK** e o **MySQL** instalados, além do projeto da API disponível. Configure a conexão do banco para o ambiente local; não publique senhas nem credenciais no repositório.

No PowerShell, entre na pasta do projeto da API e defina a conexão para a sessão atual:

```powershell
cd "CAMINHO\PARA\umfg.programacaoiii.minimalapi.2026"
$env:ASPNETCORE_ENVIRONMENT = "Development"
$env:ConnectionString = Read-Host "Informe a connection string do MySQL (mantenha-a privada)"
dotnet dev-certs https --trust
dotnet run --no-launch-profile --urls "https://localhost:7063;http://localhost:5207"
```

Troque `CAMINHO\PARA\...` pelo local da API. Informe uma connection string válida para seu banco local quando o terminal solicitar; ela ficará apenas na sessão atual. Nunca a publique no repositório. A API esperada pelo front-end fica em `https://localhost:7063`. Mantenha essa janela do terminal aberta enquanto usa a aplicação.

### 2. Front-end

Abra esta pasta no VS Code, instale a extensão **Live Server** e use **Go Live** (ou clique com o botão direito em `index.html` → **Open with Live Server**). A página será aberta no navegador, normalmente em `http://127.0.0.1:5500`.

O front-end está configurado para usar a API em `https://localhost:7063`, conforme `js/app.js`. Se alterar a porta da API, atualize também a constante `API_URL` nesse arquivo.

> **Sem conexão?** Confira se MySQL e API estão em execução e se o certificado HTTPS local foi confiado. A tela pode abrir sem a API, mas as operações de lembretes não estarão disponíveis.

## 🗺️ Rotas utilizadas

| Método | Rota | Ação |
|---|---|---|
| `GET` | `/lembretes/filtro` | Lista, filtra, ordena e pagina lembretes |
| `POST` | `/lembretes` | Cria um lembrete |
| `GET` | `/lembretes/{id}` | Consulta um lembrete |
| `PUT` | `/lembretes/{id}` | Atualiza a descrição |
| `DELETE` | `/lembretes/{id}` | Desativa um lembrete |
| `GET` | `/lembretes/imprimir` | Gera o relatório em PDF |

## 🎓 Créditos acadêmicos

- **Estudante responsável:** Felps.
- **Disciplina:** Programação III — Front-end.
- **Instituição:** UMFG.
- **Ano letivo:** 2026.

O projeto é uma atividade acadêmica independente. **Counter-Strike**, **Counter-Strike 1.6** e demais marcas relacionadas pertencem aos seus respectivos titulares. A Valve é mencionada como criadora/publicadora relacionada ao jogo, sem associação, patrocínio ou aprovação deste projeto. A interface e os textos deste trabalho são próprios e não reproduzem materiais oficiais do jogo.

## 🔗 Referências

- [Counter-Strike na Steam](https://store.steampowered.com/app/10/CounterStrike/) — página oficial do jogo.
- [Valve](https://www.valvesoftware.com/) — site oficial da empresa.
- [Documentação do .NET](https://learn.microsoft.com/dotnet/) — plataforma usada na API.
- [MySQL Documentation](https://dev.mysql.com/doc/) — documentação do banco de dados.

---

<div align="center">

**Boa missão. Comunique, planeje e jogue pelo objetivo.**  
*Projeto acadêmico — não oficial e sem vínculo com a Valve.*

</div>
