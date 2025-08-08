// 规则表达式系统实现
package rule

import (
	"github.com/Knetic/govaluate"
	"errors"
)

type Rule struct {
	ID         string            // 规则唯一标识
	Expr       string            // 表达式内容
	Priority   int               // 优先级
	SubRules   []*Rule           // 可嵌套子规则
	ProviderID string            // 关联渠道商ID
}

type RuleManager struct {
	rules map[string]*Rule
}

func NewRuleManager() *RuleManager {
	return &RuleManager{
		rules: make(map[string]*Rule),
	}
}

func (rm *RuleManager) AddRule(r *Rule) {
	rm.rules[r.ID] = r
}

func (rm *RuleManager) RemoveRule(id string) {
	delete(rm.rules, id)
}

func (rm *RuleManager) GetRule(id string) (*Rule, bool) {
	r, ok := rm.rules[id]
	return r, ok
}

func (rm *RuleManager) ListRules() []*Rule {
	list := make([]*Rule, 0, len(rm.rules))
	for _, r := range rm.rules {
		list = append(list, r)
	}
	return list
}

// 规则表达式评估
func (r *Rule) Evaluate(params map[string]interface{}) (bool, error) {
	expr, err := govaluate.NewEvaluableExpression(r.Expr)
	if err != nil {
		return false, err
	}
	result, err := expr.Evaluate(params)
	if err != nil {
		return false, err
	}
	boolResult, ok := result.(bool)
	if !ok {
		return false, errors.New("表达式结果不是布尔值")
	}
	// 若有子规则，需全部通过
	for _, sub := range r.SubRules {
		subResult, err := sub.Evaluate(params)
		if err != nil || !subResult {
			return false, err
		}
	}
	return boolResult, nil
}