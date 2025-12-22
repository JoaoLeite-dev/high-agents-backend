# High Agents Backend

Backend para o agente de IA do desafio técnico High Agents AI. Implementa um SDR digital para clínicas com memória, base de conhecimento e function calling.

## Funcionalidades

- **Agente Conversacional**: Guia o paciente através de 5 etapas de agendamento.
- **Memória**: Short-term (contexto da conversa) e long-term (via Pinecone).
- **Base de Conhecimento**: RAG com embeddings para FAQs.
- **Function Calling**: Integração com funções externas para agendamento.
- **Slot Filling**: Preenchimento automático de variáveis.

## Stack

- **Backend**: .NET 8, C#
- **LLMs**: OpenAI GPT-4o
- **Vector DB**: Pinecone
- **Orquestração**: Manual em C#

## Estrutura do Fluxo

1. Recepção inicial do paciente
2. Coleta do nome e tipo de procedimento desejado
3. Confirmação da unidade e horários disponíveis
4. Verificação de disponibilidade
5. Agendamento (simulado ou real)

## Estratégia de Memória/Contexto

- **Short-term**: Mantido na instância da conversa (lista de mensagens).
- **Long-term**: Resumos armazenados no Pinecone para recuperação contextual.

## Lista de Funções Implementadas

- `check_availability`: Verifica horários disponíveis.
- `schedule_appointment`: Agenda procedimento.
- `send_confirmation`: Envia mensagem de confirmação.

## Prompt Base do Agente

Ver em `Services/AgentService.cs` - inclui instruções para guiar a conversa, slot filling e uso de funções.

## Instalação e Execução

1. Clone o repositório.
2. Copie `.env.example` para `.env` e preencha as chaves.
3. Configure Pinecone: Crie um índice com dimensão 1536.
4. Execute `dotnet run`.

## Endpoints

- `POST /api/chat/start`: Inicia conversa.
- `POST /api/chat/send`: Envia mensagem.
- `GET /api/chat/{id}`: Obtém conversa.

## Arquivos

- Código fonte em `Controllers/`, `Services/`, `Models/`.
- Configurações em `appsettings.json`.
- Exemplo de env em `.env.example`.