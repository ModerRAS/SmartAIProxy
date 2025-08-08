// 单元测试：规则筛选模块
package rule

import (
	"testing"
	"github.com/stretchr/testify/assert"
)

func TestSelectBestProvider_Normal(t *testing.T) {
	rm := NewRuleManager()
	r1 := &Rule{ID: "r1", Expr: "a > 1", Priority: 2, ProviderID: "p1"}
	r2 := &Rule{ID: "r2", Expr: "a < 1", Priority: 1, ProviderID: "p2"}
	rm.AddRule(r1)
	rm.AddRule(r2)
	params := map[string]interface{}{"a": 2}
	id, err := rm.SelectBestProvider(params)
	assert.NoError(t, err)
	assert.Equal(t, "p1", id)
}

func TestSelectBestProvider_AllFail(t *testing.T) {
	rm := NewRuleManager()
	r1 := &Rule{ID: "r1", Expr: "a < 1", Priority: 2, ProviderID: "p1"}
	rm.AddRule(r1)
	params := map[string]interface{}{"a": 2}
	id, err := rm.SelectBestProvider(params)
	assert.NoError(t, err)
	assert.Equal(t, "", id)
}

func TestSelectBestProvider_ExprError(t *testing.T) {
	rm := NewRuleManager()
	r1 := &Rule{ID: "r1", Expr: "a >", Priority: 2, ProviderID: "p1"}
	rm.AddRule(r1)
	params := map[string]interface{}{"a": 2}
	id, err := rm.SelectBestProvider(params)
	assert.NoError(t, err)
	assert.Equal(t, "", id)
}