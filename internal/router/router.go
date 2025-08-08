// 智能路由与资金优化核心模块
package router

import (
	"SmartAIProxy/internal/provider"
	"SmartAIProxy/internal/rule"
	"sort"
	"sync"
)

// 路由器，聚合渠道商与规则管理
type Router struct {
	providerMgr *provider.ProviderManager
	ruleMgr     *rule.RuleManager
	mu          sync.RWMutex
}

// 路由选择结果
type RouteResult struct {
	Provider *provider.Provider
	Reason   string // 选择原因说明
}

// 新建路由器
func NewRouter(pm *provider.ProviderManager, rm *rule.RuleManager) *Router {
	return &Router{
		providerMgr: pm,
		ruleMgr:     rm,
	}
}

// 动态选择最优渠道商，资金利用率最大化
// params: 请求参数（如用户类型、时段、额度需求等）
func (r *Router) SelectBestProvider(params map[string]interface{}) (*RouteResult, error) {
	r.mu.RLock()
	defer r.mu.RUnlock()

	providers := r.providerMgr.ListProviders()
	if len(providers) == 0 {
		return nil, ErrNoProvider
	}

	// 1. 过滤可用渠道（状态、额度）
	candidates := make([]*provider.Provider, 0)
	for _, p := range providers {
		if p.Status != provider.StatusActive || p.Quota <= 0 {
			continue
		}
		candidates = append(candidates, p)
	}
	if len(candidates) == 0 {
		return nil, ErrNoAvailableProvider
	}

	// 2. 规则优先过滤（如特殊时段折扣、免费额度等）
	rules := r.ruleMgr.ListRules()
	ruleMatched := make(map[string]bool)
	for _, ruleObj := range rules {
		ok, _ := ruleObj.Evaluate(params)
		if ok && ruleObj.ProviderID != "" {
			ruleMatched[ruleObj.ProviderID] = true
		}
	}

	// 3. 排序：优先免费额度、特殊折扣、优先级高、价格低
	sort.SliceStable(candidates, func(i, j int) bool {
		pi, pj := candidates[i], candidates[j]
		// 优先免费额度
		if pi.Price == 0 && pj.Price != 0 {
			return true
		}
		if pj.Price == 0 && pi.Price != 0 {
			return false
		}
		// 优先规则匹配
		if ruleMatched[pi.ID] && !ruleMatched[pj.ID] {
			return true
		}
		if ruleMatched[pj.ID] && !ruleMatched[pi.ID] {
			return false
		}
		// 优先折扣高
		if pi.Discount > pj.Discount {
			return true
		}
		if pj.Discount > pi.Discount {
			return false
		}
		// 优先级高
		if pi.Priority > pj.Priority {
			return true
		}
		if pj.Priority > pi.Priority {
			return false
		}
		// 价格低
		return pi.Price < pj.Price
	})

	// 4. 返回最优渠道
	best := candidates[0]
	reason := "最优渠道自动选择"
	if ruleMatched[best.ID] {
		reason = "规则优先匹配"
	} else if best.Price == 0 {
		reason = "免费额度优先"
	} else if best.Discount > 0 {
		reason = "特殊折扣优先"
	}
	return &RouteResult{
		Provider: best,
		Reason:   reason,
	}, nil
}

// 错误定义
var (
	ErrNoProvider         = &RouterError{"无可用渠道商"}
	ErrNoAvailableProvider = &RouterError{"所有渠道额度已用尽或不可用"}
)

type RouterError struct {
	Msg string
}

func (e *RouterError) Error() string {
	return e.Msg
}

// 核心接口：根据请求参数与当前渠道状态自动选择最优渠道商
// 用于外部调用
func (r *Router) Route(params map[string]interface{}) (*provider.Provider, error) {
	result, err := r.SelectBestProvider(params)
	if err != nil {
		return nil, err
	}
	return result.Provider, nil
}