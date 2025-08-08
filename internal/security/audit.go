// Package security 提供日志安全（敏感信息脱敏、异常审计）核心接口
package security

import (
	"regexp"
	"time"
)

// Desensitize 对敏感信息进行脱敏处理
func Desensitize(input string) string {
	// 手机号脱敏
	phoneRe := regexp.MustCompile(`1[3-9]\d{9}`)
	input = phoneRe.ReplaceAllStringFunc(input, func(s string) string {
		return s[:3] + "****" + s[7:]
	})
	// 邮箱脱敏
	emailRe := regexp.MustCompile(`([a-zA-Z0-9_.+-]+)@([a-zA-Z0-9-]+\.[a-zA-Z0-9-.]+)`)
	input = emailRe.ReplaceAllString(input, "$1***@$2")
	// 身份证号脱敏
	idRe := regexp.MustCompile(`\d{6}(19|20)?\d{2}\d{2}\d{2}\d{3}[\dXx]`)
	input = idRe.ReplaceAllStringFunc(input, func(s string) string {
		return s[:6] + "********" + s[len(s)-4:]
	})
	return input
}

// AuditEvent 审计事件结构
type AuditEvent struct {
	Time      time.Time
	Event     string
	Principal string
	Detail    string
}

// NewAuditEvent 创建审计事件
func NewAuditEvent(event, principal, detail string) *AuditEvent {
	return &AuditEvent{
		Time:      time.Now(),
		Event:     event,
		Principal: principal,
		Detail:    Desensitize(detail),
	}
}