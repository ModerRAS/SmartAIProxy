// 单元测试：规则表达式模块
package rule

import (
	"testing"
	"github.com/stretchr/testify/assert"
)

func TestRuleManager_CRUD(t *testing.T) {
	rm := NewRuleManager()
	r := &Rule{ID: "r1", Expr: "1 == 1", Priority: 1}
	rm.AddRule(r)
	got, ok := rm.GetRule("r1")
	assert.True(t, ok)
	assert.Equal(t, r, got)

	rm.RemoveRule("r1")
	got, ok = rm.GetRule("r1")
	assert.False(t, ok)
	assert.Nil(t, got)
}

func TestRuleManager_ListRules_Empty(t *testing.T) {
	rm := NewRuleManager()
	list := rm.ListRules()
	assert.Empty(t, list)
}

func TestRule_Evaluate_Normal(t *testing.T) {
	r := &Rule{Expr: "a > 1"}
	params := map[string]interface{}{"a": 2}
	ok, err := r.Evaluate(params)
	assert.NoError(t, err)
	assert.True(t, ok)
}

func TestRule_Evaluate_Error(t *testing.T) {
	r := &Rule{Expr: "a >"}
	params := map[string]interface{}{"a": 2}
	ok, err := r.Evaluate(params)
	assert.Error(t, err)
	assert.False(t, ok)
}

func TestRule_Evaluate_SubRules(t *testing.T) {
	sub := &Rule{Expr: "b == 2"}
	r := &Rule{Expr: "a > 1", SubRules: []*Rule{sub}}
	params := map[string]interface{}{"a": 2, "b": 2}
	ok, err := r.Evaluate(params)
	assert.NoError(t, err)
	assert.True(t, ok)
}

func TestRule_Evaluate_TypeError(t *testing.T) {
	r := &Rule{Expr: "a + 1"}
	params := map[string]interface{}{"a": "str"}
	ok, err := r.Evaluate(params)
	assert.Error(t, err)
	assert.False(t, ok)
}