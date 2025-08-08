// 单元测试：渠道商管理模块
package provider

import (
	"testing"
	"github.com/stretchr/testify/assert"
)

func TestProviderManager_CRUD(t *testing.T) {
	pm := NewProviderManager()
	p := &Provider{ID: "p1", Type: ProviderOpenAI, Quota: 100, Price: 1, Priority: 1, Discount: 0.5, Status: StatusActive}
	pm.AddProvider(p)
	got, ok := pm.GetProvider("p1")
	assert.True(t, ok)
	assert.Equal(t, p, got)

	pm.RemoveProvider("p1")
	got, ok = pm.GetProvider("p1")
	assert.False(t, ok)
	assert.Nil(t, got)
}

func TestProviderManager_ListProviders_Empty(t *testing.T) {
	pm := NewProviderManager()
	list := pm.ListProviders()
	assert.Empty(t, list)
}