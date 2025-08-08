// 渠道商模型与管理逻辑实现
package provider

type ProviderType string

const (
	ProviderOpenAI  ProviderType = "openai"
	ProviderClaude  ProviderType = "claude"
	ProviderGemini  ProviderType = "gemini"
)

type ProviderStatus int

const (
	StatusActive   ProviderStatus = 1
	StatusInactive ProviderStatus = 0
)

type Provider struct {
	ID         string        // 唯一标识
	Type       ProviderType  // 渠道类型
	Quota      float64       // 额度
	Price      float64       // 单价
	Priority   int           // 优先级
	Discount   float64       // 折扣（0-1）
	Status     ProviderStatus// 状态
	Meta       map[string]interface{} // 其他扩展字段
}

// 渠道商管理器
type ProviderManager struct {
	providers map[string]*Provider
}

func NewProviderManager() *ProviderManager {
	return &ProviderManager{
		providers: make(map[string]*Provider),
	}
}

func (pm *ProviderManager) AddProvider(p *Provider) {
	pm.providers[p.ID] = p
}

func (pm *ProviderManager) RemoveProvider(id string) {
	delete(pm.providers, id)
}

func (pm *ProviderManager) GetProvider(id string) (*Provider, bool) {
	p, ok := pm.providers[id]
	return p, ok
}

func (pm *ProviderManager) ListProviders() []*Provider {
	list := make([]*Provider, 0, len(pm.providers))
	for _, p := range pm.providers {
		list = append(list, p)
	}
	return list
}