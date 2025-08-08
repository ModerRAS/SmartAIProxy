// 规则与渠道商自动筛选逻辑
package rule

import (
	"sort"
)

// 筛选最优渠道商ID，按优先级和表达式评估结果
func (rm *RuleManager) SelectBestProvider(params map[string]interface{}) (string, error) {
	rules := rm.ListRules()
	// 按优先级降序排序
	sort.Slice(rules, func(i, j int) bool {
		return rules[i].Priority > rules[j].Priority
	})
	for _, r := range rules {
		ok, err := r.Evaluate(params)
		if err != nil {
			continue
		}
		if ok {
			return r.ProviderID, nil
		}
	}
	return "", nil // 未找到匹配渠道
}