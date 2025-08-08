// 单元测试：智能路由模块
package router

import (
	"testing"
	"github.com/stretchr/testify/assert"
	"SmartAIProxy/internal/provider"
	"SmartAIProxy/internal/rule"
)

func mockProvider(id string, quota float64, price float64, priority int, discount float64, status provider.ProviderStatus) *provider.Provider {
	return &provider.Provider{
		ID:       id,
		Type:     provider.ProviderOpenAI,
		Quota:    quota,
		Price:    price,
		Priority: priority,
		Discount: discount,
		Status:   status,
	}
}

func TestRouter_SelectBestProvider_Normal(t *testing.T) {
	pm := provider.NewProviderManager()
	rm := rule.NewRuleManager()
	pm.AddProvider(mockProvider("p1", 100, 0, 2, 0.5, provider.StatusActive))
	pm.AddProvider(mockProvider("p2", 100, 1, 1, 0.2, provider.StatusActive))
	r := NewRouter(pm, rm)
	params := map[string]interface{}{}
	result, err := r.SelectBestProvider(params)
	assert.NoError(t, err)
	assert.NotNil(t, result)
	assert.Equal(t, "p1", result.Provider.ID)
}

func TestRouter_SelectBestProvider_NoProvider(t *testing.T) {
	pm := provider.NewProviderManager()
	rm := rule.NewRuleManager()
	r := NewRouter(pm, rm)
	params := map[string]interface{}{}
	result, err := r.SelectBestProvider(params)
	assert.Error(t, err)
	assert.Nil(t, result)
}

func TestRouter_SelectBestProvider_AllQuotaUsed(t *testing.T) {
	pm := provider.NewProviderManager()
	rm := rule.NewRuleManager()
	pm.AddProvider(mockProvider("p1", 0, 1, 1, 0, provider.StatusActive))
	r := NewRouter(pm, rm)
	params := map[string]interface{}{}
	result, err := r.SelectBestProvider(params)
	assert.Error(t, err)
	assert.Nil(t, result)
}

func TestRouter_Route_Normal(t *testing.T) {
	pm := provider.NewProviderManager()
	rm := rule.NewRuleManager()
	pm.AddProvider(mockProvider("p1", 100, 1, 1, 0, provider.StatusActive))
	r := NewRouter(pm, rm)
	params := map[string]interface{}{}
	prov, err := r.Route(params)
	assert.NoError(t, err)
	assert.NotNil(t, prov)
}