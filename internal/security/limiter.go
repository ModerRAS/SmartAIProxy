// Package security 提供流量控制（限流、黑白名单）核心接口
package security

import (
	"sync"
	"time"
)

type Limiter struct {
	rate      int           // 每秒允许请求数
	tokens    int
	lastCheck time.Time
	mu        sync.Mutex
	blacklist map[string]struct{}
	whitelist map[string]struct{}
}

// NewLimiter 创建限流器
func NewLimiter(rate int) *Limiter {
	return &Limiter{
		rate:      rate,
		tokens:    rate,
		lastCheck: time.Now(),
		blacklist: make(map[string]struct{}),
		whitelist: make(map[string]struct{}),
	}
}

// Allow 检查 Token/IP 是否允许访问
func (l *Limiter) Allow(id string) bool {
	l.mu.Lock()
	defer l.mu.Unlock()

	if _, ok := l.blacklist[id]; ok {
		return false
	}
	if len(l.whitelist) > 0 {
		if _, ok := l.whitelist[id]; !ok {
			return false
		}
	}

	now := time.Now()
	elapsed := now.Sub(l.lastCheck).Seconds()
	l.tokens += int(elapsed * float64(l.rate))
	if l.tokens > l.rate {
		l.tokens = l.rate
	}
	l.lastCheck = now

	if l.tokens > 0 {
		l.tokens--
		return true
	}
	return false
}

// AddToBlacklist 添加到黑名单
func (l *Limiter) AddToBlacklist(id string) {
	l.mu.Lock()
	defer l.mu.Unlock()
	l.blacklist[id] = struct{}{}
}

// AddToWhitelist 添加到白名单
func (l *Limiter) AddToWhitelist(id string) {
	l.mu.Lock()
	defer l.mu.Unlock()
	l.whitelist[id] = struct{}{}
}

// RemoveFromBlacklist 移除黑名单
func (l *Limiter) RemoveFromBlacklist(id string) {
	l.mu.Lock()
	defer l.mu.Unlock()
	delete(l.blacklist, id)
}

// RemoveFromWhitelist 移除白名单
func (l *Limiter) RemoveFromWhitelist(id string) {
	l.mu.Lock()
	defer l.mu.Unlock()
	delete(l.whitelist, id)
}