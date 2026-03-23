"use client";

import { InputHTMLAttributes, forwardRef } from "react";
import { cn } from "@/lib/utils";

interface CheckboxProps
  extends Omit<InputHTMLAttributes<HTMLInputElement>, "type"> {
  label: string;
  error?: string;
}

const Checkbox = forwardRef<HTMLInputElement, CheckboxProps>(
  ({ className, label, error, id, ...props }, ref) => {
    return (
      <div>
        <label className="flex items-center gap-2 cursor-pointer">
          <input
            ref={ref}
            type="checkbox"
            id={id}
            aria-invalid={error ? true : undefined}
            className={cn(
              "h-4 w-4 rounded border-gray-300 text-blue-600 transition-colors",
              "focus:ring-2 focus:ring-blue-200 focus:ring-offset-0",
              error && "border-red-300",
              className
            )}
            {...props}
          />
          <span className="text-sm text-gray-700">{label}</span>
        </label>
        {error && (
          <p className="mt-1 text-sm text-red-600" role="alert">
            {error}
          </p>
        )}
      </div>
    );
  }
);

Checkbox.displayName = "Checkbox";

export default Checkbox;
