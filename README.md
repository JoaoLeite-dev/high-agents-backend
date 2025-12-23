# High Agents Backend

Backend para o agente de IA do desafio técnico High Agents AI. Implementa um SDR digital para clínicas com memória, base de conhecimento e function calling.

## 🚀 Consumo da API (Frontend)

A API está preparada para ser consumida por aplicações frontend. Todas as respostas seguem um formato consistente com `success`, `message` e dados específicos.

### Configuração CORS

A API está configurada com CORS para permitir requisições de diferentes origens:

- **Desenvolvimento**: Permite qualquer origem (`*`)
- **Produção**: Configurado via `appsettings.json` ou variáveis de ambiente

### Endpoints Disponíveis

#### 1. Health Check
```http
GET /api/chat/health
```

**Resposta:**
```json
{
  "status": "healthy",
  "timestamp": "2025-12-23T10:30:00Z"
}
```

#### 2. Iniciar Conversa
```http
POST /api/chat/start
```

**Resposta:**
```json
{
  "success": true,
  "conversationId": "550e8400-e29b-41d4-a716-446655440000",
  "message": "Conversa iniciada com sucesso"
}
```

#### 3. Enviar Mensagem
```http
POST /api/chat/send
Content-Type: application/json

{
  "conversationId": "550e8400-e29b-41d4-a716-446655440000",
  "message": "Olá, gostaria de agendar uma consulta"
}
```

**Resposta de Sucesso:**
```json
{
  "success": true,
  "message": "Olá! Sou o assistente da clínica. Como posso ajudar você hoje?",
  "conversationId": "550e8400-e29b-41d4-a716-446655440000"
}
```

**Resposta de Erro:**
```json
{
  "success": false,
  "message": "ConversationId é obrigatório"
}
```

#### 4. Obter Conversa
```http
GET /api/chat/{conversationId}
```

**Resposta:**
```json
{
  "success": true,
  "conversation": {
    "id": "550e8400-e29b-41d4-a716-446655440000",
    "messages": [...],
    "slots": {...},
    "currentStep": 1,
    "isCompleted": false
  }
}
```

#### 5. Teste da API
```http
POST /api/chat/test
Content-Type: application/json

{
  "conversationId": "test-123",
  "message": "Mensagem de teste"
}
```

**Resposta:**
```json
{
  "success": true,
  "received": true,
  "conversationId": "test-123",
  "message": "Mensagem de teste",
  "timestamp": "2025-12-23T10:30:00Z"
}
```

### Exemplo de Integração Frontend

```javascript
// Iniciar conversa
const startConversation = async () => {
  const response = await fetch('http://localhost:5073/api/chat/start', {
    method: 'POST',
    headers: {
      'Content-Type': 'application/json'
    }
  });
  
  const data = await response.json();
  if (data.success) {
    console.log('Conversa iniciada:', data.conversationId);
    return data.conversationId;
  }
};

// Enviar mensagem
const sendMessage = async (conversationId, message) => {
  const response = await fetch('http://localhost:5073/api/chat/send', {
    method: 'POST',
    headers: {
      'Content-Type': 'application/json'
    },
    body: JSON.stringify({
      conversationId,
      message
    })
  });
  
  const data = await response.json();
  return data;
};
```

### Configuração para Produção

1. **CORS**: Configure as origens permitidas no `appsettings.json`:
```json
{
  "CORS": {
    "AllowedOrigins": [
      "https://seudominio.com",
      "https://app.seudominio.com"
    ]
  }
}
```

2. **HTTPS**: Sempre use HTTPS em produção
3. **Rate Limiting**: Considere implementar rate limiting para proteger a API
4. **Autenticação**: Adicione autenticação se necessário

## Funcionalidades

- **Agente Conversacional**: Guia o paciente através de 5 etapas de agendamento médico
- **Memória**: Short-term (contexto da conversa) e long-term (via Pinecone)
- **Base de Conhecimento**: RAG com embeddings para FAQs e conhecimento médico
- **Function Calling**: Integração com funções externas para agendamento (check_availability, schedule_appointment, send_confirmation)
- **Slot Filling**: Preenchimento automático de variáveis (nome, procedimento, unidade, data, horário)
- **Fallback para Humano**: Redirecionamento inteligente quando o agente não consegue lidar
- **Tratamento de Quota**: Fallback automático para GPT-3.5-turbo quando quota do GPT-4o é excedida
- **Testes**: Testes unitários básicos com xUnit e Moq

## Tratamento de Quota da OpenAI

O sistema implementa tratamento robusto para limitações de quota da API da OpenAI:

- **Fallback Automático**: Quando o GPT-4o atinge quota, automaticamente tenta usar GPT-3.5-turbo
- **Mensagens Informativas**: Usuário recebe mensagens claras sobre problemas de quota
- **Continuidade do Serviço**: Sistema continua funcionando mesmo com limitações de quota

### Soluções para Quota Excedida:

1. **Verificar Créditos**: Acesse [platform.openai.com](https://platform.openai.com) e verifique seus créditos
2. **Adicionar Créditos**: Compre créditos adicionais na plataforma
3. **Usar Chave Alternativa**: Configure uma chave diferente no `.env`
4. **Fallback Automático**: O sistema já usa GPT-3.5-turbo como alternativa

## Stack Tecnológico

- **Backend**: .NET 8, C#
- **LLMs**: OpenAI GPT-4o (com fallback automático para GPT-3.5-turbo)
- **Vector DB**: Pinecone para memória de longo prazo
- **Orquestração**: Implementação manual em C# com function calling
- **Logging**: Console.WriteLine para simplicidade e confiabilidade
- **Testes**: xUnit e Moq para testes unitários
- **APIs**: RESTful com Swagger/OpenAPI

## Estrutura do Fluxo de Agendamento

1. **Recepção inicial**: Saudação e apresentação do assistente
2. **Coleta de dados**: Nome do paciente e tipo de procedimento
3. **Confirmação de unidade**: Validação da unidade desejada
4. **Verificação de disponibilidade**: Consulta de horários disponíveis
5. **Agendamento final**: Confirmação e envio de notificações

## Estratégia de Memória e Contexto

- **Short-term**: Mantido na instância da conversa (lista de mensagens)
- **Long-term**: Resumos armazenados no Pinecone para recuperação contextual
- **RAG**: Busca semântica em base de conhecimento médico
- **Long-term**: Resumos armazenados no Pinecone para recuperação contextual.

## Lista de Funções Implementadas

- `check_availability`: Verifica horários disponíveis (mock).
- `schedule_appointment`: Agenda procedimento (mock).
- `send_confirmation`: Envia mensagem de confirmação (mock).

## Prompt Base do Agente

Ver em `Services/AgentService.cs` - inclui instruções para guiar a conversa, slot filling e uso de funções.

## Instalação e Execução

1. Clone o repositório.
2. Copie `.env.example` para `.env` e preencha as chaves.
3. Configure Pinecone: Crie um índice com dimensão 1536.
4. Execute `dotnet run`.

## Testes

Execute `dotnet test` para rodar os testes unitários.

## Endpoints

- `POST /api/chat/start`: Inicia uma conversa.
- `POST /api/chat/send`: Envia mensagem e recebe resposta.
- `GET /api/chat/{conversationId}`: Obtém detalhes da conversa.

## Arquivos

- Código fonte em `Controllers/`, `Services/`, `Models/`.
- Configurações em `appsettings.json`.
- Exemplo de env em `.env.example`.
