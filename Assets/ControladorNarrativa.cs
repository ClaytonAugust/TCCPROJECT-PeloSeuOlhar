using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ControladorNarrativa : MonoBehaviour
{
    [System.Serializable]
    private class RegistroEscolha
    {
        public string nomeCenario;
        public string nomeQuadro;
        public int numeroOpcao;
        public string textoOpcao;
        public string consequencia;
        public int pontuacao;

        public RegistroEscolha(
            string cenario,
            string quadro,
            int numero,
            string opcao,
            string efeito,
            int pontos)
        {
            nomeCenario = cenario;
            nomeQuadro = quadro;
            numeroOpcao = numero;
            textoOpcao = opcao;
            consequencia = efeito;
            pontuacao = pontos;
        }
    }

    [Header("Menu Inicial")]
    [Tooltip("Painel inicial que contém o botão Jogar. Deixe vazio se não houver menu inicial nesta cena.")]
    [SerializeField] private GameObject painelMenuInicial;

    [Header("Lista de cenários")]
    [SerializeField] private List<DadosCenario> cenarios = new List<DadosCenario>();

    [Header("Elementos da narrativa")]
    [SerializeField] private Image imagemQuadro;
    [SerializeField] private TMP_Text tituloQuadro;
    [SerializeField] private TMP_Text textoQuadro;

    [Header("Imagens de conclusão")]
    [Tooltip("Imagem exibida quando um cenário termina, antes de voltar à seleção ou ir à reflexão final.")]
    [SerializeField] private Sprite imagemConclusaoCenario;
    [Tooltip("Imagem exibida na tela da reflexão final do jogo.")]
    [SerializeField] private Sprite imagemReflexaoFinal;

    [Header("Escolhas")]
    [SerializeField] private GameObject painelEscolhas;
    [SerializeField] private Button[] botoesOpcoes;

    [Header("Continuar")]
    [SerializeField] private Button botaoContinuar;

    [Header("Voltar para a seleção")]
    [SerializeField] private Button botaoVoltar;

    [Header("Tela de seleção")]
    [SerializeField] private GameObject painelSelecaoCenarios;
    [SerializeField] private GameObject painelNarrativa;
    [SerializeField] private Button[] botoesSelecaoCenarios;
    [SerializeField] private Image[] imagensCapaCenarios;
    [SerializeField] private GameObject[] iconesCadeado;
    [SerializeField] private TMP_Text[] titulosCenarios;

    [Header("Tela de reflexão final")]
    [SerializeField] private GameObject painelReflexaoFinal;
    [SerializeField] private TMP_Text tituloReflexaoFinal;
    [SerializeField] private TMP_Text textoReflexaoFinal;
    [SerializeField] private Button botaoVoltarMenuFinal;

    private bool[] cenariosDesbloqueados;
    private int cenarioAtual;
    private int quadroAtual;
    private int proximoQuadroEscolhido = -1;
    private int opcaoSelecionada = -1;
    private bool aguardandoProximoCenario;
    private bool jogoFinalizado;

    // Respostas da tentativa atual e respostas das histórias concluídas.
    private readonly List<RegistroEscolha> respostasJogador = new List<RegistroEscolha>();
    private readonly Dictionary<int, List<RegistroEscolha>> respostasPorCenario =
        new Dictionary<int, List<RegistroEscolha>>();
    private List<RegistroEscolha> respostasCenarioAtual = new List<RegistroEscolha>();
    private bool respostasCenarioAtualSalvas;

    private void Start()
    {
        if (cenarios == null || cenarios.Count == 0)
        {
            Debug.LogError("ControladorNarrativa: a lista Cenários está vazia. Adicione os assets DadosCenario no Inspector.");
            return;
        }

        if (painelSelecaoCenarios == null)
            Debug.LogError("ControladorNarrativa: arraste PainelSelecaoCenarios para o campo correspondente no Inspector.");

        if (painelNarrativa == null)
            Debug.LogError("ControladorNarrativa: arraste PainelNarrativa para o campo correspondente no Inspector.");

        if (botaoContinuar != null)
        {
            botaoContinuar.onClick.RemoveListener(Continuar);
            botaoContinuar.onClick.AddListener(Continuar);
        }
        else
        {
            Debug.LogError("ControladorNarrativa: o campo BotaoContinuar não foi preenchido no Inspector.");
        }

        if (botaoVoltar != null)
        {
            botaoVoltar.onClick.RemoveListener(VoltarParaSelecao);
            botaoVoltar.onClick.AddListener(VoltarParaSelecao);
        }

        cenariosDesbloqueados = new bool[cenarios.Count];
        cenariosDesbloqueados[0] = true; // O primeiro cenário começa desbloqueado.

        ConfigurarBotoesSelecao();

        // O controlador deve ficar em um GameObject ativo que não seja um destes painéis.
        if (painelNarrativa != null)
            painelNarrativa.SetActive(false);

        if (painelMenuInicial != null)
        {
            painelMenuInicial.SetActive(true);
            if (painelSelecaoCenarios != null)
                painelSelecaoCenarios.SetActive(false);
        }
        else if (painelSelecaoCenarios != null)
        {
            // Sem menu inicial configurado, abre a seleção diretamente.
            painelSelecaoCenarios.SetActive(true);
        }

        if (painelReflexaoFinal != null)
            painelReflexaoFinal.SetActive(false);

        if (botaoVoltarMenuFinal != null)
        {
            botaoVoltarMenuFinal.onClick.RemoveListener(
                VoltarAoMenuInicialDaReflexao
            );

            botaoVoltarMenuFinal.onClick.AddListener(
                VoltarAoMenuInicialDaReflexao
            );
        }

        AtualizarTelaSelecao();
    }

    private void ConfigurarBotoesSelecao()
    {
        if (botoesSelecaoCenarios == null)
        {
            Debug.LogError("ControladorNarrativa: o array BotoesSelecaoCenarios não foi configurado.");
            return;
        }

        for (int i = 0; i < botoesSelecaoCenarios.Length; i++)
        {
            Button botao = botoesSelecaoCenarios[i];
            if (botao == null)
            {
                Debug.LogWarning("ControladorNarrativa: o botão de seleção no índice " + i + " está vazio.");
                continue;
            }

            int indice = i;
            botao.onClick.RemoveAllListeners();
            botao.onClick.AddListener(() => IniciarCenario(indice));
        }
    }

    // Vincule este método ao OnClick do botão Jogar do menu inicial.
    public void AbrirSelecaoCenarios()
    {
        if (painelMenuInicial != null)
            painelMenuInicial.SetActive(false);

        if (painelNarrativa != null)
            painelNarrativa.SetActive(false);

        if (painelSelecaoCenarios != null)
            painelSelecaoCenarios.SetActive(true);
        else
            Debug.LogError("Não é possível abrir a seleção: PainelSelecaoCenarios não foi atribuído.");

        AtualizarTelaSelecao();
    }

    private void AtualizarTelaSelecao()
    {
        if (botoesSelecaoCenarios == null)
            return;

        for (int i = 0; i < botoesSelecaoCenarios.Length; i++)
        {
            Button botao = botoesSelecaoCenarios[i];
            if (botao == null)
                continue;

            if (cenarios == null || i >= cenarios.Count || cenarios[i] == null)
            {
                botao.gameObject.SetActive(false);
                continue;
            }

            bool desbloqueado = cenariosDesbloqueados != null &&
                                i < cenariosDesbloqueados.Length &&
                                cenariosDesbloqueados[i];

            botao.gameObject.SetActive(true);
            botao.interactable = desbloqueado;

            if (titulosCenarios != null &&
                i < titulosCenarios.Length &&
                titulosCenarios[i] != null)
            {
                titulosCenarios[i].text = cenarios[i].nomeCenario;
            }

            if (imagensCapaCenarios != null && i < imagensCapaCenarios.Length && imagensCapaCenarios[i] != null)
            {
                Image imagem = imagensCapaCenarios[i];
                imagem.sprite = cenarios[i].imagemCapa;
                imagem.color = desbloqueado
                    ? Color.white
                    : new Color(0.25f, 0.25f, 0.25f, 1f);
            }

            if (iconesCadeado != null && i < iconesCadeado.Length && iconesCadeado[i] != null)
                iconesCadeado[i].SetActive(!desbloqueado);
        }
    }

    private void IniciarCenario(int indice)
    {
        if (cenarios == null || indice < 0 || indice >= cenarios.Count)
            return;

        if (cenariosDesbloqueados == null || indice >= cenariosDesbloqueados.Length || !cenariosDesbloqueados[indice])
            return;

        DadosCenario cenario = cenarios[indice];
        if (cenario == null || cenario.quadros == null || cenario.quadros.Length == 0)
        {
            Debug.LogError("O cenário no índice " + indice + " não possui quadros configurados.");
            return;
        }

        cenarioAtual = indice;
        quadroAtual = 0;
        opcaoSelecionada = -1;
        proximoQuadroEscolhido = -1;
        aguardandoProximoCenario = false;
        jogoFinalizado = false;

        // Só substituímos as respostas anteriores se esta nova tentativa for concluída.
        // Assim, abandonar uma repetição não apaga os dados de uma história já concluída.
        respostasCenarioAtual = new List<RegistroEscolha>();
        respostasCenarioAtualSalvas = false;

        if (painelMenuInicial != null)
            painelMenuInicial.SetActive(false);
        if (painelSelecaoCenarios != null)
            painelSelecaoCenarios.SetActive(false);
        if (painelNarrativa != null)
            painelNarrativa.SetActive(true);

        MostrarQuadro();
    }

    private void VoltarParaSelecao()
    {
        if (jogoFinalizado || aguardandoProximoCenario)
            return;

        if (painelNarrativa != null)
            painelNarrativa.SetActive(false);
        if (painelSelecaoCenarios != null)
            painelSelecaoCenarios.SetActive(true);

        AtualizarTelaSelecao();
    }

    private void MostrarQuadro()
    {
        if (cenarioAtual < 0 || cenarioAtual >= cenarios.Count)
        {
            Debug.LogError("Índice do cenário inválido: " + cenarioAtual);
            return;
        }

        DadosCenario cenario = cenarios[cenarioAtual];
        if (cenario == null || cenario.quadros == null)
        {
            Debug.LogError("O cenário ou seu array de quadros não foi configurado.");
            return;
        }

        // Um índice igual ao tamanho do array significa fim do cenário.
        if (quadroAtual == cenario.quadros.Length)
        {
            FinalizarCenario();
            return;
        }

        if (quadroAtual < 0 || quadroAtual > cenario.quadros.Length)
        {
            Debug.LogError("Índice de quadro inválido (" + quadroAtual + ") no cenário '" + cenario.nomeCenario + "'. Verifique os valores Proximo Quadro nas opções.");
            return;
        }

        DadosCenario.Quadro quadro = cenario.quadros[quadroAtual];
        if (quadro == null)
        {
            Debug.LogError("O quadro no índice " + quadroAtual + " do cenário '" + cenario.nomeCenario + "' está vazio.");
            return;
        }

        if (tituloQuadro != null)
            tituloQuadro.text = quadro.titulo;
        if (textoQuadro != null)
            textoQuadro.text = quadro.texto;

        if (imagemQuadro != null)
        {
            imagemQuadro.sprite = quadro.imagem;
            imagemQuadro.gameObject.SetActive(quadro.imagem != null);
        }

        opcaoSelecionada = -1;
        proximoQuadroEscolhido = -1;
        aguardandoProximoCenario = false;

        int totalOpcoes = quadro.opcoes == null ? 0 : quadro.opcoes.Length;
        bool quadroTemEscolha = quadro.exigeEscolha && totalOpcoes > 0;

        if (quadro.exigeEscolha && totalOpcoes == 0)
            Debug.LogWarning("O quadro '" + quadro.titulo + "' exige escolha, mas não tem opções. Ele será tratado como um quadro de narrativa.");

        if (botaoContinuar != null)
        {
            botaoContinuar.gameObject.SetActive(!quadroTemEscolha);
            botaoContinuar.interactable = !quadroTemEscolha;
            DefinirTextoBotao("Continuar");
        }

        // O botão Voltar leva à seleção e não aparece durante a escolha.
        if (botaoVoltar != null)
            botaoVoltar.gameObject.SetActive(!quadroTemEscolha);

        if (painelEscolhas != null)
            painelEscolhas.SetActive(quadroTemEscolha);

        if (botoesOpcoes == null)
            return;

        for (int i = 0; i < botoesOpcoes.Length; i++)
        {
            Button botao = botoesOpcoes[i];
            if (botao == null)
                continue;

            botao.onClick.RemoveAllListeners();

            if (i < totalOpcoes && quadroTemEscolha)
            {
                botao.gameObject.SetActive(true);
                botao.interactable = true;

                TMP_Text textoBotao = botao.GetComponentInChildren<TMP_Text>(true);
                if (textoBotao != null)
                    textoBotao.text = quadro.opcoes[i].textoOpcao;

                int indice = i;
                botao.onClick.AddListener(() => SelecionarOpcao(indice));
            }
            else
            {
                botao.gameObject.SetActive(false);
            }
        }
    }

    private void SelecionarOpcao(int indice)
    {
        DadosCenario cenario = cenarios[cenarioAtual];
        DadosCenario.Quadro quadro = cenario.quadros[quadroAtual];

        if (quadro.opcoes == null || indice < 0 || indice >= quadro.opcoes.Length)
            return;

        // Clicar na opção já selecionada desmarca a escolha.
        if (opcaoSelecionada == indice)
        {
            opcaoSelecionada = -1;
            proximoQuadroEscolhido = -1;

            if (textoQuadro != null)
                textoQuadro.text = quadro.texto;
            if (botaoContinuar != null)
            {
                botaoContinuar.gameObject.SetActive(false);
                botaoContinuar.interactable = false;
            }
            if (botaoVoltar != null)
                botaoVoltar.gameObject.SetActive(false);

            if (botoesOpcoes != null)
            {
                foreach (Button botao in botoesOpcoes)
                    if (botao != null) botao.interactable = true;
            }
            return;
        }

        opcaoSelecionada = indice;
        DadosCenario.Opcao opcao = quadro.opcoes[indice];
        proximoQuadroEscolhido = opcao.proximoQuadro;

        // A consequência narrativa completa será exibida no quadro indicado por
        // opcao.proximoQuadro quando o jogador clicar em Continuar.
        // Não substituímos o texto do quadro inicial nesta etapa.

        if (botaoContinuar != null)
        {
            botaoContinuar.gameObject.SetActive(true);
            botaoContinuar.interactable = true;
        }

        // O botão Voltar fica oculto enquanto a resposta estiver selecionada.
        if (botaoVoltar != null)
            botaoVoltar.gameObject.SetActive(false);

        // Deixa as opções interativas para que o jogador possa trocar de resposta.
        if (botoesOpcoes != null)
        {
            foreach (Button botao in botoesOpcoes)
                if (botao != null) botao.interactable = true;
        }
    }

    private void Continuar()
    {
        if (jogoFinalizado || cenarios == null || cenarios.Count == 0)
            return;

        if (aguardandoProximoCenario)
        {
            if (cenarioAtual < cenarios.Count - 1)
            {
                // Concluir um cenário desbloqueia o próximo na sequência.
                int proximoCenario = cenarioAtual + 1;
                if (cenariosDesbloqueados != null && proximoCenario < cenariosDesbloqueados.Length)
                    cenariosDesbloqueados[proximoCenario] = true;

                if (painelNarrativa != null)
                    painelNarrativa.SetActive(false);
                if (painelSelecaoCenarios != null)
                    painelSelecaoCenarios.SetActive(true);

                AtualizarTelaSelecao();
            }
            else
            {
                // A reflexão geral só aparece ao terminar o último cenário.
                FinalizarJogo();
            }
            return;
        }

        DadosCenario cenario = cenarios[cenarioAtual];
        if (cenario == null || cenario.quadros == null || quadroAtual < 0 || quadroAtual >= cenario.quadros.Length)
        {
            Debug.LogError("Não foi possível avançar: cenário ou quadro inválido.");
            return;
        }

        DadosCenario.Quadro quadro = cenario.quadros[quadroAtual];
        if (quadro == null)
        {
            Debug.LogError("O quadro atual está vazio.");
            return;
        }

        if (opcaoSelecionada >= 0)
        {
            DadosCenario.Opcao opcao = quadro.opcoes[opcaoSelecionada];

            // O valor pode apontar para um quadro válido ou para quadros.Length (fim da história).
            if (proximoQuadroEscolhido < 0 || proximoQuadroEscolhido > cenario.quadros.Length)
            {
                Debug.LogError(
                    "A opção '" + opcao.textoOpcao + "' no quadro '" + quadro.titulo +
                    "' tem Proximo Quadro inválido: " + proximoQuadroEscolhido +
                    ". Use um índice entre 0 e " + cenario.quadros.Length +
                    " (" + cenario.quadros.Length + " significa fim do cenário)."
                );
                return;
            }

            // A pontuação vem da configuração individual da opção no Inspector.
            // Assim, a ordem visual das opções não altera a avaliação.
            int pontuacao = opcao.pontuacao;
            RegistroEscolha registro = new RegistroEscolha(
                cenario.nomeCenario,
                quadro.titulo,
                opcaoSelecionada + 1,
                opcao.textoOpcao,
                opcao.textoConsequencia,
                pontuacao
            );
            respostasCenarioAtual.Add(registro);
            quadroAtual = proximoQuadroEscolhido;
        }
        else
        {
            // Quadros sem escolha podem seguir em sequência ou apontar para
            // um quadro específico (por exemplo, para convergir os caminhos).
            if (quadro.definirProximoQuadroManualmente)
            {
                int destino = quadro.proximoQuadroSemEscolha;

                if (destino < 0 || destino > cenario.quadros.Length)
                {
                    Debug.LogError(
                        "O quadro '" + quadro.titulo + "' tem Proximo Quadro Sem Escolha inválido: " + destino +
                        ". Use um índice entre 0 e " + cenario.quadros.Length +
                        " (o tamanho do array significa fim do cenário)."
                    );
                    return;
                }

                quadroAtual = destino;
            }
            else
            {
                // Com a opção desmarcada, mantém o comportamento antigo.
                quadroAtual++;
            }
        }

        MostrarQuadro();
    }

    private void FinalizarCenario()
    {
        aguardandoProximoCenario = true;

        // Salva as escolhas apenas quando a história é concluída.
        if (!respostasCenarioAtualSalvas)
        {
            respostasPorCenario[cenarioAtual] =
                new List<RegistroEscolha>(respostasCenarioAtual);
            ReconstruirRespostasJogador();
            respostasCenarioAtualSalvas = true;
        }

        if (painelEscolhas != null)
            painelEscolhas.SetActive(false);

        // Reutiliza a mesma área visual dos quadros narrativos para evitar
        // uma tela vazia/azul ao terminar cada cenário.
        if (imagemQuadro != null)
        {
            Sprite imagemConclusao = cenarios[cenarioAtual] != null
                ? cenarios[cenarioAtual].imagemConclusao
                : null;

            // Permite uma imagem diferente para cada cenário; se não houver,
            // usa a imagem geral configurada no ControladorNarrativa.
            if (imagemConclusao == null)
                imagemConclusao = imagemConclusaoCenario;

            if (imagemConclusao != null)
                imagemQuadro.sprite = imagemConclusao;

            imagemQuadro.gameObject.SetActive(imagemQuadro.sprite != null);
        }

        if (tituloQuadro != null)
            tituloQuadro.text = "História concluída";

        if (cenarioAtual < cenarios.Count - 1)
        {
            if (textoQuadro != null)
            {
                textoQuadro.text =
                    "Você concluiu esta história. Continue para voltar à tela de seleção. " +
                    "O próximo cenário será desbloqueado.";
            }
            DefinirTextoBotao("Voltar à seleção");
        }
        else
        {
            if (textoQuadro != null)
            {
                textoQuadro.text =
                    "Você concluiu a última história. Continue para ver sua reflexão final, " +
                    "baseada nas escolhas feitas durante o jogo.";
            }
            DefinirTextoBotao("Ver reflexão final");
        }

        if (botaoContinuar != null)
        {
            botaoContinuar.gameObject.SetActive(true);
            botaoContinuar.interactable = true;
        }
        if (botaoVoltar != null)
            botaoVoltar.gameObject.SetActive(false);
    }

    private void ReconstruirRespostasJogador()
    {
        respostasJogador.Clear();

        // Mantém uma ordem previsível: cenário 1, 2, 3...
        for (int i = 0; i < cenarios.Count; i++)
        {
            List<RegistroEscolha> respostas;
            if (respostasPorCenario.TryGetValue(i, out respostas))
                respostasJogador.AddRange(respostas);
        }
    }

    private void FinalizarJogo()
    {
        jogoFinalizado = true;

        // Esconde os painéis anteriores.
        if (painelNarrativa != null)
            painelNarrativa.SetActive(false);

        if (painelSelecaoCenarios != null)
            painelSelecaoCenarios.SetActive(false);

        if (painelMenuInicial != null)
            painelMenuInicial.SetActive(false);

        if (painelEscolhas != null)
            painelEscolhas.SetActive(false);

        if (painelReflexaoFinal != null)
        {
            // Preenche os elementos do novo painel.
            if (tituloReflexaoFinal != null)
                tituloReflexaoFinal.text =
                    "Sua jornada pelo olhar do outro";

            if (textoReflexaoFinal != null)
                textoReflexaoFinal.text = GerarFeedbackFinal();

            // Exibe a reflexão em tela inteira.
            painelReflexaoFinal.SetActive(true);
        }
        else
        {
            Debug.LogError(
                "PainelReflexaoFinal não foi atribuído no Inspector."
            );

            // Mantém a reflexão antiga como alternativa de segurança.
            if (painelNarrativa != null)
                painelNarrativa.SetActive(true);

            if (imagemQuadro != null)
                imagemQuadro.gameObject.SetActive(false);

            if (tituloQuadro != null)
                tituloQuadro.text = "Sua reflexão final";

            if (textoQuadro != null)
                textoQuadro.text = GerarFeedbackFinal();

            if (botaoContinuar != null)
                botaoContinuar.gameObject.SetActive(false);

            if (botaoVoltar != null)
                botaoVoltar.gameObject.SetActive(false);
        }

        RegistrarRespostasNoConsole();
    }

    private string GerarFeedbackFinal()
    {
        if (respostasJogador.Count == 0)
        {
            return "Você concluiu as histórias. Pense em como ouvir as pessoas idosas e respeitar " +
                   "suas decisões pode contribuir para relações mais respeitosas entre gerações.";
        }

        int totalPontos = 0;
        List<string> cenariosPositivos = new List<string>();
        List<string> cenariosReflexao = new List<string>();
        Dictionary<string, int> pontosPorCenario = new Dictionary<string, int>();

        foreach (RegistroEscolha registro in respostasJogador)
        {
            totalPontos += registro.pontuacao;
            if (!pontosPorCenario.ContainsKey(registro.nomeCenario))
                pontosPorCenario[registro.nomeCenario] = 0;
            pontosPorCenario[registro.nomeCenario] += registro.pontuacao;
        }

        foreach (KeyValuePair<string, int> item in pontosPorCenario)
        {
            if (item.Value > 0)
                cenariosPositivos.Add(item.Key);
            else if (item.Value < 0)
                cenariosReflexao.Add(item.Key);
        }

        float media = (float)totalPontos / respostasJogador.Count;
        StringBuilder mensagem = new StringBuilder();

        if (media >= 1f)
        {
            mensagem.Append(
                "Em suas escolhas, apareceu uma tendência a considerar a autonomia, a escuta e a participação " +
                "das pessoas idosas nas decisões. Continue valorizando o diálogo e oferecendo apoio sem presumir " +
                "que a idade impede alguém de decidir por si."
            );
        }
        else if (media <= -0.5f)
        {
            mensagem.Append(
                "Suas escolhas abrem espaço para refletir sobre como a intenção de ajudar pode, em algumas " +
                "situações, acabar retirando a autonomia de uma pessoa idosa. Antes de decidir por alguém, procure " +
                "ouvir o que essa pessoa pensa, deseja e consegue fazer, oferecendo apoio sem presumir suas limitações."
            );
        }
        else
        {
            mensagem.Append(
                "Suas escolhas apresentaram diferentes formas de lidar com as situações. Uma reflexão importante " +
                "é equilibrar o cuidado com a autonomia: perguntar antes de ajudar, ouvir a pessoa e considerar " +
                "sua opinião nas decisões."
            );
        }

        if (cenariosPositivos.Count > 0)
        {
            mensagem.Append("\n\nNos cenários ");
            mensagem.Append(string.Join(", ", cenariosPositivos));
            mensagem.Append(", suas escolhas favoreceram a participação ou a autonomia da pessoa idosa.");
        }

        if (cenariosReflexao.Count > 0)
        {
            mensagem.Append("\n\nNos cenários ");
            mensagem.Append(string.Join(", ", cenariosReflexao));
            mensagem.Append(", vale refletir sobre como incluir mais a pessoa nas decisões e ouvir sua versão.");
        }

        mensagem.Append(
            "\n\nEste retorno é uma reflexão baseada nas alternativas escolhidas durante o jogo, " +
            "não um diagnóstico ou uma avaliação psicológica."
        );

        return mensagem.ToString();
    }

    private void RegistrarRespostasNoConsole()
    {
        Debug.Log("===== REGISTRO DAS ESCOLHAS =====");
        foreach (RegistroEscolha registro in respostasJogador)
        {
            Debug.Log(
                "Cenário: " + registro.nomeCenario +
                "\nQuadro: " + registro.nomeQuadro +
                "\nOpção: " + registro.numeroOpcao +
                "\nTexto: " + registro.textoOpcao +
                "\nConsequência: " + registro.consequencia +
                "\nIndicador reflexivo: " + registro.pontuacao
            );
        }
        Debug.Log("===== FIM DO REGISTRO =====");
    }

    private void DefinirTextoBotao(string texto)
    {
        if (botaoContinuar == null)
            return;

        TMP_Text textoBotao = botaoContinuar.GetComponentInChildren<TMP_Text>(true);
        if (textoBotao != null)
            textoBotao.text = texto;
    }

    public void VoltarParaMenuInicial()
    {
        if (painelSelecaoCenarios != null)
            painelSelecaoCenarios.SetActive(false);

        if (painelMenuInicial != null)
            painelMenuInicial.SetActive(true);
    }

    public void VoltarAoMenuInicialDaReflexao()
    {
        if (painelReflexaoFinal != null)
            painelReflexaoFinal.SetActive(false);

        if (painelNarrativa != null)
            painelNarrativa.SetActive(false);

        if (painelSelecaoCenarios != null)
            painelSelecaoCenarios.SetActive(false);

        if (painelMenuInicial != null)
            painelMenuInicial.SetActive(true);
        else
            Debug.LogError(
                "PainelMenuInicial não foi atribuído no Inspector."
            );
    }
}